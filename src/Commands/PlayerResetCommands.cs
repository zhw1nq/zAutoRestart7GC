using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using zAutoRestart7GC.Config;
using zAutoRestart7GC.Services;

namespace zAutoRestart7GC.Commands;

public class PlayerResetCommands
{
  private readonly ISwiftlyCore _core;
  private readonly IVoteExtendManager _voteExtend;
  private readonly IMessageFormatter _formatter;
  private readonly IOptionsMonitor<AutoRestartConfig> _config;
  private readonly ILogger<PlayerResetCommands> _logger;

  public PlayerResetCommands(
      ISwiftlyCore core,
      IVoteExtendManager voteExtend,
      IMessageFormatter formatter,
      IOptionsMonitor<AutoRestartConfig> config,
      ILogger<PlayerResetCommands> logger)
  {
    _core = core;
    _voteExtend = voteExtend;
    _formatter = formatter;
    _config = config;
    _logger = logger;
  }

  [Command("ereset", registerRaw: true)]
  public void ExtendResetCommand(ICommandContext context)
  {
    if (!context.IsSentByPlayer)
    {
      context.Reply("This command can only be used by players.");
      return;
    }

    var requiredPerm = _config.CurrentValue.Permissions.ExtendReset;
    if (!string.IsNullOrWhiteSpace(requiredPerm) && !PermissionHelper.HasPermissionOrWildcard(_core, context.Sender, requiredPerm))
    {
      context.Reply(_formatter.FormatChat("error.no_permission"));
      return;
    }

    var player = context.Sender!;
    if (!_voteExtend.TryVote(player, out var feedbackKey, out var feedbackArgs))
    {
      context.Reply(_formatter.FormatChat(feedbackKey, feedbackArgs));
    }
  }
}

