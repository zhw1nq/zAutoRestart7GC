using zAutoRestart7GC.Enums;

namespace zAutoRestart7GC.Services;

public interface IRestartManager
{
  RestartState State { get; }
  DateTime? TargetResetTime { get; }
  TimeSpan? GetRemainingTime();

  void Initialize();
  void Shutdown();

  void SetTemporarySchedule(TimeSpan duration, string adminName, ulong adminSteamId);
  bool RemoveTemporarySchedule(string adminName, ulong adminSteamId);

  void TriggerHardReset(string adminName, ulong adminSteamId);
  void StartCountdown(string reason = "Scheduled restart");
  void Start8SecondsCountdown(string reason = "Scheduled restart");
  void OnMatchEnd();
}
