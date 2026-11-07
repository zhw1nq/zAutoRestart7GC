using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using zAutoRestart7GC.Config;
using zAutoRestart7GC.Cron;
using zAutoRestart7GC.Services;

namespace zAutoRestart7GC.Commands;

public class AdminResetCommands
{
  private readonly ISwiftlyCore _core;
  private readonly IRestartManager _restartManager;
  private readonly IMessageFormatter _formatter;
  private readonly IOptionsMonitor<AutoRestartConfig> _config;
  private readonly ILogger<AdminResetCommands> _logger;

  public AdminResetCommands(
      ISwiftlyCore core,
      IRestartManager restartManager,
      IMessageFormatter formatter,
      IOptionsMonitor<AutoRestartConfig> config,
      ILogger<AdminResetCommands> logger)
  {
    _core = core;
    _restartManager = restartManager;
    _formatter = formatter;
    _config = config;
    _logger = logger;
  }

  [Command("hreset", registerRaw: true)]
  public void HardResetCommand(ICommandContext context)
  {
    var requiredPerm = _config.CurrentValue.Permissions.HardReset;
    if (!PermissionHelper.HasPermissionOrWildcard(_core, context.Sender, requiredPerm))
    {
      context.Reply(_formatter.FormatChat("error.no_permission"));
      return;
    }

    string adminName = context.IsSentByPlayer ? context.Sender!.Name : "Console";
    ulong adminSteamId = context.IsSentByPlayer ? context.Sender!.SteamID : 0;

    _logger.LogInformation("Hard reset invoked by {Admin} ({SteamId}).", adminName, adminSteamId);
    _restartManager.TriggerHardReset(adminName, adminSteamId);
  }

  [Command("sreset", registerRaw: true)]
  public void SetResetCommand(ICommandContext context)
  {
    var requiredPerm = _config.CurrentValue.Permissions.SetSchedule;
    if (!PermissionHelper.HasPermissionOrWildcard(_core, context.Sender, requiredPerm))
    {
      context.Reply(_formatter.FormatChat("error.no_permission"));
      return;
    }

    if (context.Args.Length == 0)
    {
      context.Reply(_formatter.FormatChat("chat.sreset.usage"));
      return;
    }

    string adminName = context.IsSentByPlayer ? context.Sender!.Name : "Console";
    ulong adminSteamId = context.IsSentByPlayer ? context.Sender!.SteamID : 0;
    string arg = context.Args[0].Trim();

    if (arg.Equals("remove", StringComparison.OrdinalIgnoreCase) || arg.Equals("cancel", StringComparison.OrdinalIgnoreCase))
    {
      if (!_restartManager.RemoveTemporarySchedule(adminName, adminSteamId))
      {
        context.Reply(_formatter.FormatChat("chat.sreset.no_temp"));
      }
      return;
    }

    if (!TimeStringParser.TryParse(arg, out var duration))
    {
      context.Reply(_formatter.FormatChat("chat.sreset.invalid_time"));
      return;
    }

    _restartManager.SetTemporarySchedule(duration, adminName, adminSteamId);
  }
}

