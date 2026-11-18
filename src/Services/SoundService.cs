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

    float volume = _config.CurrentValue.NotificationSoundVolume > 0 ? _config.CurrentValue.NotificationSoundVolume : 1.0f;
    float pitch = _config.CurrentValue.NotificationSoundPitch > 0 ? _config.CurrentValue.NotificationSoundPitch : 1.0f;

    // Emit via SwiftlyS2 SoundEvent API with SourceEntityIndex = -1 (emits at recipient location)
    try
    {
      using var soundEvent = new SoundEvent
      {
        Name = sound,
        SourceEntityIndex = -1,
        Volume = volume,
        Pitch = pitch
      };

      soundEvent.Recipients.AddAllPlayers();
      soundEvent.Emit();
      return;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "SoundEvent.Emit failed for '{Sound}'. Attempting client command fallback.", sound);
    }

    // Fallback: If SoundEvent failed (e.g. direct file path), execute client 'play' command
    try
    {
      var players = _core.PlayerManager.GetAllValidPlayers();
      foreach (var player in players)
      {
        if (player.IsValid && !player.IsFakeClient)
        {
          player.ExecuteCommand($"play \"{sound}\"");
        }
      }
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Fallback player.ExecuteCommand('play') failed for sound '{Sound}'.", sound);
    }
  }
}
