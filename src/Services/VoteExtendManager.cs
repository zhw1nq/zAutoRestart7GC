using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using zAutoRestart7GC.Config;

namespace zAutoRestart7GC.Services;

public class VoteExtendManager : IVoteExtendManager
{
  private readonly ISwiftlyCore _core;
  private readonly IOptionsMonitor<AutoRestartConfig> _config;
  private readonly IMessageFormatter _formatter;
  private readonly IWebhookService _webhook;
  private readonly ISoundService _soundService;
  private readonly ILogger<VoteExtendManager> _logger;

  private readonly HashSet<ulong> _votedSteamIds = new();
  private readonly Dictionary<int, ulong> _voterPlayerIds = new();
  private int _cycleExtendCount;

  public event Action<int>? OnExtendApproved;
  public Func<TimeSpan?>? GetRemainingTimeFunc { get; set; }
  public Func<DateTime?>? GetScheduledTimeFunc { get; set; }

  public VoteExtendManager(
      ISwiftlyCore core,
      IOptionsMonitor<AutoRestartConfig> config,
      IMessageFormatter formatter,
      IWebhookService webhook,
      ISoundService soundService,
      ILogger<VoteExtendManager> logger)
  {
    _core = core;
    _config = config;
    _formatter = formatter;
    _webhook = webhook;
    _soundService = soundService;
    _logger = logger;
  }

  public int CurrentCycleExtendCount => _cycleExtendCount;

  public bool CanVoteNow(out string reasonKey, out object[] reasonArgs)
  {
    reasonArgs = Array.Empty<object>();

    var extendConfig = _config.CurrentValue.Extend;

    var eligible = GetEligiblePlayers();
    if (eligible.Count < extendConfig.MinPlayersToVote)
    {
      reasonKey = "chat.ereset.min_players";
      reasonArgs = [extendConfig.MinPlayersToVote];
      return false;
    }

    if (extendConfig.MaxExtendsPerCycle > 0 && _cycleExtendCount >= extendConfig.MaxExtendsPerCycle)
    {
      reasonKey = "chat.ereset.max_reached";
      reasonArgs = [extendConfig.MaxExtendsPerCycle];
      return false;
    }

    var remaining = GetRemainingTimeFunc?.Invoke();
    if (remaining == null)
    {
      reasonKey = "chat.ereset.not_scheduled";
      return false;
    }

    if (remaining.Value.TotalMinutes > extendConfig.AllowedWindowMinutes || remaining.Value.TotalSeconds <= 8)
    {
      reasonKey = "chat.ereset.not_scheduled";
      return false;
    }

    reasonKey = string.Empty;
    return true;
  }

  public bool TryVote(IPlayer player, out string feedbackKey, out object[] feedbackArgs)
  {
    feedbackArgs = Array.Empty<object>();

    if (!CanVoteNow(out var notAllowedKey, out var notAllowedArgs))
    {
      feedbackKey = notAllowedKey;
      feedbackArgs = notAllowedArgs;
      return false;
    }

    if (_votedSteamIds.Contains(player.SteamID))
    {
      feedbackKey = "chat.ereset.already_voted";
      return false;
    }

    _votedSteamIds.Add(player.SteamID);
    _voterPlayerIds[player.PlayerID] = player.SteamID;

    _logger.LogInformation("Player {Name} ({SteamId}) voted to extend restart.", player.Name, player.SteamID);

    CheckVoteProgress(player.Name);

    feedbackKey = string.Empty;
    return true;
  }

  private List<IPlayer> GetEligiblePlayers()
  {
    return _core.PlayerManager.GetAllValidPlayers()
        .Where(p => p.IsValid && !p.IsFakeClient)
        .ToList();
  }

  private void CheckVoteProgress(string? lastVoterName = null)
  {
    var eligiblePlayers = GetEligiblePlayers();
    int total = eligiblePlayers.Count;
    if (total == 0) return;

    var extendConfig = _config.CurrentValue.Extend;
    int neededVotes = (int)Math.Ceiling(total * (extendConfig.RequiredPercentage / 100.0));
    if (neededVotes <= 0) neededVotes = 1;

    int currentVotes = _votedSteamIds.Count;
    double percentage = Math.Round((double)currentVotes / total * 100.0, 1);

    if (currentVotes >= neededVotes)
    {
      TriggerExtendSuccess(total, currentVotes);
    }
    else if (!string.IsNullOrEmpty(lastVoterName))
    {
      _formatter.BroadcastChat("chat.ereset.voted", lastVoterName, currentVotes, neededVotes, percentage);
    }
  }

  private void TriggerExtendSuccess(int totalHumans, int currentVotes)
  {
    var extendConfig = _config.CurrentValue.Extend;
    _cycleExtendCount++;

    _logger.LogInformation("Vote extension passed! ({Votes}/{Total}). Extending by {Minutes} minutes.",
        currentVotes, totalHumans, extendConfig.ExtendMinutes);

    // Clear active votes for this cycle
    _votedSteamIds.Clear();
    _voterPlayerIds.Clear();

    // Trigger callback to update target time
    OnExtendApproved?.Invoke(extendConfig.ExtendMinutes);

    var newTime = GetScheduledTimeFunc?.Invoke() ?? DateTime.Now.AddMinutes(extendConfig.ExtendMinutes);
    var formattedTime = newTime.ToString("HH:mm:ss");

    _soundService.PlayNotificationSound();
    _formatter.BroadcastChat("chat.ereset.passed", extendConfig.ExtendMinutes, formattedTime);

    _ = _webhook.SendExtendVoteAsync(extendConfig.ExtendMinutes, newTime, currentVotes, totalHumans);
  }

  public void OnClientDisconnected(int playerId)
  {
    if (_voterPlayerIds.TryGetValue(playerId, out var steamId))
    {
      _voterPlayerIds.Remove(playerId);
      _votedSteamIds.Remove(steamId);
      _logger.LogInformation("Voter with SteamID {SteamId} disconnected. Vote removed.", steamId);
    }

    // Check if remaining voters now meet >= 80%
    if (_votedSteamIds.Count > 0 && CanVoteNow(out _, out _))
    {
      CheckVoteProgress();
    }
  }

  public void ResetVotes()
  {
    _votedSteamIds.Clear();
    _voterPlayerIds.Clear();
    _cycleExtendCount = 0;
  }
}

