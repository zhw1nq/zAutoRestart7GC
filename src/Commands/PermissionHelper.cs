using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace zAutoRestart7GC.Commands;

public static class PermissionHelper
{
  public static bool HasPermissionOrWildcard(ISwiftlyCore core, IPlayer? player, string requiredPermission)
  {
    // Console commands always have permission
    if (player == null) return true;

    // Empty permission string means everyone has access
    if (string.IsNullOrWhiteSpace(requiredPermission)) return true;

    ulong steamId = player.SteamID;

    // Check direct permission
    if (core.Permission.PlayerHasPermission(steamId, requiredPermission)) return true;

    // Check global wildcard "*"
    if (core.Permission.PlayerHasPermission(steamId, "*")) return true;

    // Check prefix wildcards (e.g. "admin.permission.reset.*", "admin.*")
    var parts = requiredPermission.Split('.');
    string currentPrefix = "";
    for (int i = 0; i < parts.Length - 1; i++)
    {
      currentPrefix += parts[i] + ".";
      if (core.Permission.PlayerHasPermission(steamId, currentPrefix + "*"))
      {
        return true;
      }
    }

    return false;
  }
}

