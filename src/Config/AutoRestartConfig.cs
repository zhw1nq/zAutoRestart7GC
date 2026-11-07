namespace zAutoRestart7GC.Config;

public class AutoRestartConfig
{
  public string CronSchedule { get; set; } = "0 */4 * * *";
  public List<int> WarningIntervalsMinutes { get; set; } = [30, 15, 10, 5, 1];
  public int CountdownSeconds { get; set; } = 3;
  public string NotificationSound { get; set; } = "sounds/ui/deathnotice.vsnd_c";
  public bool WaitForMatchEnd { get; set; } = true;
  public ExtendConfig Extend { get; set; } = new();
  public PermissionsConfig Permissions { get; set; } = new();
  public WebhookConfig Webhook { get; set; } = new();
}

