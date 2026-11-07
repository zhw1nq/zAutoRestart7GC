using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using zAutoRestart7GC.Services;

namespace zAutoRestart7GC.Listeners;

public class MatchEventListener : IDisposable
{
  private readonly ISwiftlyCore _core;
  private readonly IRestartManager _restartManager;
  private readonly ILogger<MatchEventListener> _logger;

  private Guid? _winPanelHookGuid;
  private Guid? _gameEndHookGuid;

  public MatchEventListener(
      ISwiftlyCore core,
      IRestartManager restartManager,
      ILogger<MatchEventListener> logger)
  {
    _core = core;
    _restartManager = restartManager;
    _logger = logger;

    RegisterHooks();
  }

  private void RegisterHooks()
  {
    try
    {
      _winPanelHookGuid = _core.GameEvent.HookPre<EventCsWinPanelMatch>(OnWinPanelMatch);
      _gameEndHookGuid = _core.GameEvent.HookPre<EventGameEnd>(OnGameEnd);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to register match end game event hooks.");
    }
  }

  private HookResult OnWinPanelMatch(EventCsWinPanelMatch @event)
  {
    _logger.LogInformation("EventCsWinPanelMatch detected.");
    _restartManager.OnMatchEnd();
    return HookResult.Continue;
  }

  private HookResult OnGameEnd(EventGameEnd @event)
  {
    _logger.LogInformation("EventGameEnd detected.");
    _restartManager.OnMatchEnd();
    return HookResult.Continue;
  }

  public void Dispose()
  {
    try
    {
      if (_winPanelHookGuid.HasValue) _core.GameEvent.Unhook(_winPanelHookGuid.Value);
      if (_gameEndHookGuid.HasValue) _core.GameEvent.Unhook(_gameEndHookGuid.Value);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Error unhooking match end game events.");
    }
  }
}

