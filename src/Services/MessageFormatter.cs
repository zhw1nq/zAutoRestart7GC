using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace zAutoRestart7GC.Services;

public class MessageFormatter : IMessageFormatter
{
  private readonly ISwiftlyCore _core;

  public MessageFormatter(ISwiftlyCore core)
  {
    _core = core;
  }

  public string GetPrefix()
  {
    var localizer = _core.Localizer;
    var prefix = localizer["prefix"];
    return string.IsNullOrEmpty(prefix) ? "[GOLD]AutoRestart [SILVER]» " : prefix;
  }

  public string FormatChat(string key, params object[] args)
  {
    var localizer = _core.Localizer;
    var msg = args.Length > 0 ? localizer[key, args] : localizer[key];
    return GetPrefix() + msg;
  }

  public string FormatHtml(string key, params object[] args)
  {
    var localizer = _core.Localizer;
    return args.Length > 0 ? localizer[key, args] : localizer[key];
  }

  public void BroadcastChat(string key, params object[] args)
  {
    var formatted = FormatChat(key, args);
    _core.PlayerManager.SendChat(formatted);
  }

  public void BroadcastCenterHtml(string key, int durationSeconds, params object[] args)
  {
    var html = FormatHtml(key, args);
    int durationMs = Math.Max(1000, durationSeconds * 1000);
    _core.PlayerManager.SendCenterHTML(html, durationMs);
  }

  public void SendPlayerChat(IPlayer player, string key, params object[] args)
  {
    if (player.IsValid)
    {
      var localizer = _core.Translation.GetPlayerLocalizer(player);
      var prefix = localizer["prefix"];
      if (string.IsNullOrEmpty(prefix)) prefix = GetPrefix();

      var msg = args.Length > 0 ? localizer[key, args] : localizer[key];
      player.SendChat(prefix + msg);
    }
  }
}

