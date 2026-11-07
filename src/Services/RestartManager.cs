using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.Scheduler;
using zAutoRestart7GC.Config;
using zAutoRestart7GC.Cron;
using zAutoRestart7GC.Enums;

namespace zAutoRestart7GC.Services;

public class RestartManager : IRestartManager
{
  private readonly ISwiftlyCore _core;
  private readonly IOptionsMonitor<AutoRestartConfig> _config;
  private readonly IMessageFormatter _formatter;
  private readonly IWebhookService _webhook;
  private readonly IVoteExtendManager _voteExtend;
  private readonly ISoundService _soundService;
  private readonly ILogger<RestartManager> _logger;

  private RestartState _state = RestartState.Idle;
  private DateTime? _targetResetTime;
  private bool _isTemporarySchedule;
  private CancellationTokenSource? _timerCts;

  private readonly HashSet<int> _notifiedIntervals = new();
  private int _countdownSecondsRemaining = 3;
  private string _restartReason = "Scheduled restart";

  public RestartState State => _state;
  public DateTime? TargetResetTime => _targetResetTime;

  public RestartManager(
      ISwiftlyCore core,
      IOptionsMonitor<AutoRestartConfig> config,
      IMessageFormatter formatter,
      IWebhookService webhook,
      IVoteExtendManager voteExtend,
      ISoundService soundService,
      ILogger<RestartManager> logger)
  {
    _core = core;
    _config = config;
    _formatter = formatter;
    _webhook = webhook;
    _voteExtend = voteExtend;
    _soundService = soundService;
    _logger = logger;

    // Connect vote extend callbacks
    if (_voteExtend is VoteExtendManager vm)
    {
      vm.GetRemainingTimeFunc = GetRemainingTime;
      vm.GetScheduledTimeFunc = () => _targetResetTime;
      vm.OnExtendApproved += ExtendSchedule;
    }

    _config.OnChange(newCfg =>
    {
      _logger.LogInformation("Configuration changed. Re-evaluating schedule...");
      if (!_isTemporarySchedule && _state == RestartState.Scheduled)
      {
        ComputeNextCronSchedule();
      }
    });
  }

  public TimeSpan? GetRemainingTime()
  {
    if (_targetResetTime == null) return null;
    var diff = _targetResetTime.Value - DateTime.Now;
    return diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
  }

  public void Initialize()
  {
    _logger.LogInformation("Initializing RestartManager...");
    ComputeNextCronSchedule();
    StartMainLoop();
  }

  public void Shutdown()
  {
    _logger.LogInformation("Shutting down RestartManager...");
    _timerCts?.Cancel();
    _timerCts = null;
    _state = RestartState.Idle;
  }

  private void ComputeNextCronSchedule()
  {
    var cronStr = _config.CurrentValue.CronSchedule;
    if (CronExpression.TryParse(cronStr, out var cron))
    {
      var next = cron!.GetNextOccurrence(DateTime.Now);
      if (next.HasValue)
      {
        _targetResetTime = next.Value;
        _isTemporarySchedule = false;
        SyncNotifiedIntervals(_targetResetTime.Value - DateTime.Now);
        _state = RestartState.Scheduled;
        _logger.LogInformation("Next restart scheduled for: {Time} (cron: {Cron})", next.Value, cronStr);
        return;
      }
    }

    _logger.LogWarning("Invalid or unfulfillable cron expression: '{Cron}'. Defaulting to +4 hours.", cronStr);
    _targetResetTime = DateTime.Now.AddHours(4);
    _isTemporarySchedule = false;
    SyncNotifiedIntervals(_targetResetTime.Value - DateTime.Now);
    _state = RestartState.Scheduled;
  }

  private void SyncNotifiedIntervals(TimeSpan remaining)
  {
    _notifiedIntervals.Clear();
    var intervals = _config.CurrentValue.WarningIntervalsMinutes;
    // Any interval strictly greater than remaining time (with a small 0.1m buffer) has already passed
    foreach (var interval in intervals)
    {
      if (interval > remaining.TotalMinutes + 0.1)
      {
        _notifiedIntervals.Add(interval);
      }
    }
  }

  private void StartMainLoop()
  {
    _timerCts?.Cancel();

    _timerCts = _core.Scheduler.AddTimer(ctx =>
    {
      try
      {
        OnTick();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in RestartManager timer tick.");
      }

      return _state == RestartState.Restarting ? TimerStep.Stop() : TimerStep.WaitForSeconds(1.0f);
    });
  }

  private void OnTick()
  {
    if (_state == RestartState.Countdown)
    {
      ProcessCountdownTick();
      return;
    }

    if (_state == RestartState.WaitingForMatchEnd || _state == RestartState.Restarting || _state == RestartState.Idle)
    {
      return;
    }

    if (_targetResetTime == null) return;

    var remaining = _targetResetTime.Value - DateTime.Now;
    int countdownThreshold = Math.Max(1, _config.CurrentValue.CountdownSeconds);

    // When countdown threshold (e.g. 3s) is reached, begin countdown
    if (remaining.TotalSeconds <= countdownThreshold)
    {
      OnScheduledTimeReached();
      return;
    }

    // Check warning intervals (30, 15, 10, 5, 1 minutes)
    var intervals = _config.CurrentValue.WarningIntervalsMinutes;
    foreach (var interval in intervals.OrderByDescending(x => x))
    {
      if (remaining.TotalMinutes <= interval && !_notifiedIntervals.Contains(interval))
      {
        _notifiedIntervals.Add(interval);
        TriggerWarningAlert(interval);
        break;
      }
    }
  }

  private void OnScheduledTimeReached()
  {
    var gameRules = _core.EntitySystem.GetGameRules();
    bool isWarmup = gameRules?.WarmupPeriod ?? false;
    int playerCount = _core.PlayerManager.GetAllValidPlayers().Count();

    if (_config.CurrentValue.WaitForMatchEnd && !isWarmup && playerCount > 0)
    {
      _state = RestartState.WaitingForMatchEnd;
      _logger.LogInformation("Scheduled restart time reached, but a match is in progress. Postponing until match end.");

      _soundService.PlayNotificationSound();
      _formatter.BroadcastChat("chat.match_in_progress");

      _ = _webhook.SendWaitingForMatchEndAsync(_targetResetTime ?? DateTime.Now);
      return;
    }

    StartCountdown("Scheduled restart");
  }

  public void OnMatchEnd()
  {
    if (_state == RestartState.WaitingForMatchEnd)
    {
      _logger.LogInformation("Match ended! Starting restart countdown.");
      StartCountdown("Match concluded");
    }
  }

  public void StartCountdown(string reason = "Scheduled restart")
  {
    int seconds = _config.CurrentValue.CountdownSeconds;
    if (seconds <= 0) seconds = 3;

    _state = RestartState.Countdown;
    _countdownSecondsRemaining = seconds;
    _restartReason = reason;

    _logger.LogInformation("Starting {Seconds}-second countdown. Reason: {Reason}", seconds, reason);
  }

  public void Start8SecondsCountdown(string reason = "Scheduled restart") => StartCountdown(reason);

  private void ProcessCountdownTick()
  {
    if (_countdownSecondsRemaining > 0)
    {
      _soundService.PlayNotificationSound();
      _formatter.BroadcastChat("chat.countdown.seconds", _countdownSecondsRemaining);
      _formatter.BroadcastCenterHtml("center.countdown.seconds", 2, _countdownSecondsRemaining);
      _countdownSecondsRemaining--;
    }
    else
    {
      _state = RestartState.Restarting;
      _formatter.BroadcastChat("chat.restarting_now");
      _formatter.BroadcastCenterHtml("center.restarting_now", 3);

      _ = _webhook.SendRestartingNowAsync(_restartReason);

      KickAllPlayers();

      _logger.LogInformation("Countdown reached zero. Executing 'quit' command...");
      _core.Engine.ExecuteCommand("quit");
    }
  }

  private void KickAllPlayers()
  {
    try
    {
      var players = _core.PlayerManager.GetAllValidPlayers().ToList();
      _logger.LogInformation("Kicking {Count} player(s) before server shutdown with NETWORK_DISCONNECT_SERVER_SHUTDOWN (69)...", players.Count);

      foreach (var player in players)
      {
        if (player.IsValid)
        {
          player.Kick("SERVER_SHUTDOWN", ENetworkDisconnectionReason.NETWORK_DISCONNECT_SERVER_SHUTDOWN);
        }
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while kicking players before restart.");
    }
  }

  public void TriggerHardReset(string adminName, ulong adminSteamId)
  {
    _logger.LogWarning("Admin {Admin} ({SteamId}) triggered a hard reset.", adminName, adminSteamId);

    _soundService.PlayNotificationSound();
    _formatter.BroadcastChat("chat.hreset.executed", adminName);

    _ = _webhook.SendHardResetAsync(adminName, adminSteamId);

    StartCountdown($"Hard reset by {adminName}");
  }

  public void SetTemporarySchedule(TimeSpan duration, string adminName, ulong adminSteamId)
  {
    _targetResetTime = DateTime.Now.Add(duration);
    _isTemporarySchedule = true;
    SyncNotifiedIntervals(duration);
    _state = RestartState.Scheduled;

    var formattedTarget = _targetResetTime.Value.ToString("HH:mm:ss");
    var durationStr = duration.TotalMinutes >= 1
        ? $"{duration.TotalMinutes:0}m"
        : $"{duration.TotalSeconds:0}s";

    _logger.LogInformation("Admin {Admin} scheduled temporary restart in {Duration} at {Time}",
        adminName, durationStr, formattedTarget);

    _soundService.PlayNotificationSound();
    _formatter.BroadcastChat("chat.sreset.scheduled", adminName, durationStr, formattedTarget);

    _ = _webhook.SendSetScheduleAsync(adminName, adminSteamId, durationStr, _targetResetTime.Value);
  }

  public bool RemoveTemporarySchedule(string adminName, ulong adminSteamId)
  {
    if (!_isTemporarySchedule)
    {
      return false;
    }

    _logger.LogInformation("Admin {Admin} removed temporary schedule. Reverting to crontab.", adminName);
    ComputeNextCronSchedule();

    _soundService.PlayNotificationSound();
    _formatter.BroadcastChat("chat.sreset.removed", adminName);

    _ = _webhook.SendRemoveScheduleAsync(adminName, adminSteamId);
    return true;
  }

  private void ExtendSchedule(int minutes)
  {
    if (_targetResetTime.HasValue)
    {
      _targetResetTime = _targetResetTime.Value.AddMinutes(minutes);
    }
    else
    {
      _targetResetTime = DateTime.Now.AddMinutes(minutes);
    }

    SyncNotifiedIntervals(_targetResetTime.Value - DateTime.Now);
    _state = RestartState.Scheduled;
  }

  public void TriggerWarningAlert(int minutes)
  {
    var formattedTarget = _targetResetTime?.ToString("HH:mm:ss") ?? "Soon";
    _logger.LogInformation("Server restart warning: {Minutes} minute(s) remaining (at {Time}).", minutes, formattedTarget);

    _soundService.PlayNotificationSound();
    _formatter.BroadcastChat("chat.warning.minutes", minutes, formattedTarget);

    if (_targetResetTime.HasValue)
    {
      _ = _webhook.SendWarningAsync(minutes, _targetResetTime.Value);
    }
  }
}
