using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using zAutoRestart7GC.Services;

namespace zAutoRestart7GC.Listeners;

public class PlayerEventListener : IDisposable
{
  private readonly ISwiftlyCore _core;
  private readonly IVoteExtendManager _voteExtend;
  private readonly ILogger<PlayerEventListener> _logger;

  public PlayerEventListener(
      ISwiftlyCore core,
      IVoteExtendManager voteExtend,
      ILogger<PlayerEventListener> logger)
  {
    _core = core;
    _voteExtend = voteExtend;
    _logger = logger;

    _core.Event.OnClientDisconnected += HandleClientDisconnected;
  }

  private void HandleClientDisconnected(IOnClientDisconnectedEvent @event)
  {
    try
    {
      _voteExtend.OnClientDisconnected(@event.PlayerId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error handling client disconnected event.");
    }
  }

  public void Dispose()
  {
    _core.Event.OnClientDisconnected -= HandleClientDisconnected;
  }
}

