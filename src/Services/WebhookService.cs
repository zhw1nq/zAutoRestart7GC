using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using zAutoRestart7GC.Config;

namespace zAutoRestart7GC.Services;

public class WebhookService : IWebhookService
{
  private static readonly HttpClient HttpClient = new();
  private readonly ISwiftlyCore _core;
  private readonly IOptionsMonitor<AutoRestartConfig> _config;
  private readonly ILogger<WebhookService> _logger;

  public WebhookService(
      ISwiftlyCore core,
      IOptionsMonitor<AutoRestartConfig> config,
      ILogger<WebhookService> logger)
  {
    _core = core;
    _config = config;
    _logger = logger;
  }

  private string GetServerName()
  {
    var overrideName = _config.CurrentValue.Webhook.ServerNameOverride;
    if (!string.IsNullOrWhiteSpace(overrideName))
    {
      return overrideName;
    }

    try
    {
      var hostnameConvar = _core.ConVar.FindAsString("hostname");
      if (hostnameConvar != null && !string.IsNullOrWhiteSpace(hostnameConvar.ValueAsString))
      {
        return hostnameConvar.ValueAsString;
      }
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to read hostname convar.");
    }

    return "CS2 Server";
  }

  private async Task PostEmbedAsync(string title, string description, int colorHex, List<object> fields)
  {
    var webhookConfig = _config.CurrentValue.Webhook;
    if (!webhookConfig.Enabled || string.IsNullOrWhiteSpace(webhookConfig.WebhookUrl))
    {
      return;
    }

    try
    {
      var payload = new
      {
        username = string.IsNullOrWhiteSpace(webhookConfig.BotUsername) ? "CS2 Server AutoRestart" : webhookConfig.BotUsername,
        avatar_url = webhookConfig.AvatarUrl,
        embeds = new[]
          {
                    new
                    {
                        title,
                        description,
                        color = colorHex,
                        fields,
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        footer = new
                        {
                            text = "zAutoRestart7GC • SwiftlyS2"
                        }
                    }
                }
      };

      var json = JsonSerializer.Serialize(payload);
      var content = new StringContent(json, Encoding.UTF8, "application/json");

      var response = await HttpClient.PostAsync(webhookConfig.WebhookUrl, content);
      if (!response.IsSuccessStatusCode)
      {
        _logger.LogWarning("Failed to post Discord webhook. Status: {StatusCode}", response.StatusCode);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Exception occurred while posting Discord webhook.");
    }
  }

  public async Task SendWarningAsync(int minutesRemaining, DateTime scheduledTime)
  {
    if (!_config.CurrentValue.Webhook.Events.NotifyOnWarning) return;

    var serverName = GetServerName();
    var unix = new DateTimeOffset(scheduledTime).ToUnixTimeSeconds();
    var fields = new List<object>
        {
            new { name = "Server", value = $"`{serverName}`", inline = true },
            new { name = "Time Remaining", value = $"**{minutesRemaining} minute(s)**", inline = true },
            new { name = "Scheduled At", value = $"<t:{unix}:F> (<t:{unix}:R>)", inline = false }
        };

    await PostEmbedAsync(
        "⚠️ Server Restart Warning",
        $"The server **{serverName}** is scheduled to restart soon.",
        16753920, // Orange
        fields
    );
  }

  public async Task SendWaitingForMatchEndAsync(DateTime scheduledTime)
  {
    if (!_config.CurrentValue.Webhook.Events.NotifyOnWaitingForMatchEnd) return;

    var serverName = GetServerName();
    var unix = new DateTimeOffset(scheduledTime).ToUnixTimeSeconds();
    var fields = new List<object>
        {
            new { name = "Server", value = $"`{serverName}`", inline = true },
            new { name = "Status", value = "Waiting for current match to conclude", inline = true },
            new { name = "Scheduled At", value = $"<t:{unix}:F>", inline = false }
        };

    await PostEmbedAsync(
        "⏳ Scheduled Restart Postponed (Match In Progress)",
        $"Server **{serverName}** reached scheduled restart time, but a match is currently active. The restart will initiate immediately upon match completion.",
        3447003, // Blue
        fields
    );
  }

  public async Task SendHardResetAsync(string adminName, ulong adminSteamId)
  {
    if (!_config.CurrentValue.Webhook.Events.NotifyOnHardReset) return;

    var serverName = GetServerName();
    var fields = new List<object>
        {
            new { name = "Server", value = $"`{serverName}`", inline = true },
            new { name = "Triggered By", value = $"**{adminName}** (`{adminSteamId}`)", inline = true },
            new { name = "Action", value = "Forced immediate 8s restart sequence", inline = false }
        };

    await PostEmbedAsync(
        "🚨 Hard Restart Initiated",
        $"An administrator triggered a forced hard reset on **{serverName}**.",
        15158332, // Red
        fields
    );
  }

  public async Task SendExtendVoteAsync(int extendedMinutes, DateTime newScheduledTime, int currentVotes, int totalPlayers)
  {
    if (!_config.CurrentValue.Webhook.Events.NotifyOnExtendVote) return;

    var serverName = GetServerName();
    var unix = new DateTimeOffset(newScheduledTime).ToUnixTimeSeconds();
    var fields = new List<object>
        {
            new { name = "Server", value = $"`{serverName}`", inline = true },
            new { name = "Votes Reached", value = $"**{currentVotes}/{totalPlayers}** players", inline = true },
            new { name = "Postponed By", value = $"**+{extendedMinutes} minutes**", inline = true },
            new { name = "New Restart Time", value = $"<t:{unix}:F> (<t:{unix}:R>)", inline = false }
        };

    await PostEmbedAsync(
        "✅ Restart Postponed by Player Vote",
        $"Players on **{serverName}** voted to postpone the server restart.",
        3066993, // Green
        fields
    );
  }

  public async Task SendSetScheduleAsync(string adminName, ulong adminSteamId, string duration, DateTime scheduledTime)
  {
    if (!_config.CurrentValue.Webhook.Events.NotifyOnSetSchedule) return;

    var serverName = GetServerName();
    var unix = new DateTimeOffset(scheduledTime).ToUnixTimeSeconds();
    var fields = new List<object>
        {
            new { name = "Server", value = $"`{serverName}`", inline = true },
            new { name = "Admin", value = $"**{adminName}** (`{adminSteamId}`)", inline = true },
            new { name = "Duration", value = $"**{duration}**", inline = true },
            new { name = "Scheduled At", value = $"<t:{unix}:F> (<t:{unix}:R>)", inline = false }
        };

    await PostEmbedAsync(
        "📅 Temporary Restart Scheduled",
        $"An administrator scheduled a temporary restart on **{serverName}**.",
        3447003, // Blue
        fields
    );
  }

  public async Task SendRemoveScheduleAsync(string adminName, ulong adminSteamId)
  {
    if (!_config.CurrentValue.Webhook.Events.NotifyOnSetSchedule) return;

    var serverName = GetServerName();
    var fields = new List<object>
        {
            new { name = "Server", value = $"`{serverName}`", inline = true },
            new { name = "Admin", value = $"**{adminName}** (`{adminSteamId}`)", inline = true },
            new { name = "Action", value = "Cancelled temporary restart schedule", inline = false }
        };

    await PostEmbedAsync(
        "🗑️ Temporary Schedule Cancelled",
        $"An administrator removed the temporary restart schedule on **{serverName}**.",
        10070709, // Grey
        fields
    );
  }

  public async Task SendRestartingNowAsync(string reason)
  {
    if (!_config.CurrentValue.Webhook.Events.NotifyOnRestarting) return;

    var serverName = GetServerName();
    var fields = new List<object>
        {
            new { name = "Server", value = $"`{serverName}`", inline = true },
            new { name = "Reason", value = $"`{reason}`", inline = true }
        };

    await PostEmbedAsync(
        "🔄 Server Restarting Now",
        $"Server **{serverName}** is shutting down for restart (`quit`).",
        15158332, // Red
        fields
    );
  }

  public async Task SendTestWebhookAsync()
  {
    var serverName = GetServerName();
    var fields = new List<object>
        {
            new { name = "Server", value = $"`{serverName}`", inline = true },
            new { name = "Status", value = "Webhook is working properly!", inline = true }
        };

    await PostEmbedAsync(
        "🧪 Webhook Test",
        $"This is a test notification from **{serverName}**.",
        3066993,
        fields
    );
  }
}

