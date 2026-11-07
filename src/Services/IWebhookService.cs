namespace zAutoRestart7GC.Services;

public interface IWebhookService
{
  Task SendWarningAsync(int minutesRemaining, DateTime scheduledTime);
  Task SendWaitingForMatchEndAsync(DateTime scheduledTime);
  Task SendHardResetAsync(string adminName, ulong adminSteamId);
  Task SendExtendVoteAsync(int extendedMinutes, DateTime newScheduledTime, int currentVotes, int totalPlayers);
  Task SendSetScheduleAsync(string adminName, ulong adminSteamId, string duration, DateTime scheduledTime);
  Task SendRemoveScheduleAsync(string adminName, ulong adminSteamId);
  Task SendRestartingNowAsync(string reason);
  Task SendTestWebhookAsync();
}

