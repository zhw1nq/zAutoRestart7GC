using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Sounds;
using zAutoRestart7GC.Config;

namespace zAutoRestart7GC.Services;

public class SoundService : ISoundService
{
  private readonly ISwiftlyCore _core;
  private readonly IOptionsMonitor<AutoRestartConfig> _config;
  private readonly ILogger<SoundService> _logger;

  public SoundService(
      ISwiftlyCore core,
      IOptionsMonitor<AutoRestartConfig> config,
      ILogger<SoundService> logger)
  {
    _core = core;
    _config = config;
    _logger = logger;
  }

  public void PlayNotificationSound()
  {
    var sound = _config.CurrentValue.NotificationSound;
    if (string.IsNullOrWhiteSpace(sound))
    {
      return;
    }

    try
    {
      using var soundEvent = new SoundEvent()
      {
        Name = sound
      };
      soundEvent.Recipients.AddAllPlayers();
      soundEvent.Emit();
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to emit sound event '{Sound}'.", sound);
    }
  }
}
