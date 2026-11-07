using SwiftlyS2.Shared.Players;

namespace zAutoRestart7GC.Services;

public interface IMessageFormatter
{
  string GetPrefix();
  string FormatChat(string key, params object[] args);
  string FormatHtml(string key, params object[] args);
  void BroadcastChat(string key, params object[] args);
  void BroadcastCenterHtml(string key, int durationSeconds, params object[] args);
  void SendPlayerChat(IPlayer player, string key, params object[] args);
}

