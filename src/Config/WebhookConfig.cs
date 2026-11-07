namespace zAutoRestart7GC.Config;

public class WebhookEventsConfig
{
  public bool NotifyOnWarning { get; set; } = true;
  public bool NotifyOnWaitingForMatchEnd { get; set; } = true;
  public bool NotifyOnHardReset { get; set; } = true;
  public bool NotifyOnExtendVote { get; set; } = true;
  public bool NotifyOnSetSchedule { get; set; } = true;
  public bool NotifyOnRestarting { get; set; } = true;
}

public class WebhookConfig
{
  public bool Enabled { get; set; } = true;
  public string WebhookUrl { get; set; } = "";
  public string BotUsername { get; set; } = "CS2 Server AutoRestart";
  public string AvatarUrl { get; set; } = "https://i.imgur.com/8QfJq1M.png";
  public string ServerNameOverride { get; set; } = "";
  public WebhookEventsConfig Events { get; set; } = new();
}

