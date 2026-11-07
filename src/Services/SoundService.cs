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

    bool isFilePath = sound.Contains('/') || sound.Contains('\\') ||
                      sound.EndsWith(".vsnd", StringComparison.OrdinalIgnoreCase) ||
                      sound.EndsWith(".vsnd_c", StringComparison.OrdinalIgnoreCase);

    // If it's a sound event name (no path/extension), try emitting via SoundEvent first
    if (!isFilePath)
    {
      try
      {
        using var soundEvent = new SoundEvent(sound, 1.0f, 1.0f);
        soundEvent.Recipients.AddAllPlayers();
        soundEvent.Emit();
        return;
      }
      catch (Exception ex)
      {
        _logger.LogDebug(ex, "SoundEvent emit failed for '{Sound}', falling back to client command.", sound);
      }
    }

    // For file paths (e.g. "sounds/ui/deathnotice.vsnd_c") or fallback:
    // Execute client-side 'play <sound>' on all connected human players.
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
      _logger.LogWarning(ex, "Error playing sound '{Sound}' via player.ExecuteCommand.", sound);
    }
  }
}

