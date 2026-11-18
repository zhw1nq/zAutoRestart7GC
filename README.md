# zAutoRestart7GC

An automatic server restart plugin for **Counter-Strike 2** running on **SwiftlyS2** (.NET 10).

---

## Features

- **Crontab Scheduling:** Configurable cron expressions (e.g., `0 */4 * * *` for every 4 hours).
- **Time Warnings:** Chat alerts at customized intervals (30m, 15m, 10m, 5m, 1m).
- **3-Second Countdown:** Visual countdown HUD in Center HTML before restart.
- **Notification SFX:** Plays custom audio cue (`zUI.Success`) on alerts and countdown ticks via Source 2 SoundEvent.
- **Match Protection:** Automatically postpones restart if a match is currently in progress.
- **Safe Player Kick:** Kicks players with engine reason code `69` (`NETWORK_DISCONNECT_SERVER_SHUTDOWN`).
- **Vote Extend (`!ereset`):** Players can vote to delay restart by X minutes (requires 80% approval and minimum 3 players).
- **Admin Commands:** Instant hard reset (`!hreset`) and temporary schedule management (`!sreset`).
- **Discord Webhook:** Sends rich embed notifications for warnings, hard resets, extensions, and restarts.
- **Multi-language:** Supports English and Vietnamese out of the box.

---

## Commands & Permissions

| Command | Permission | Description |
| :--- | :--- | :--- |
| `!hreset` | `admin.permission.reset.hard` | Forces an immediate hard restart (3s countdown) |
| `!sreset <time>` | `admin.permission.reset.set` | Schedules temporary restart (e.g., `1h`, `30m`, `45s`) |
| `!sreset remove` | `admin.permission.reset.set` | Removes temporary schedule, reverts to crontab |
| `!ereset` | *(None / Public)* | Votes to extend current restart schedule |

---

## Configuration (`config.jsonc`)

```jsonc
{
  "AutoRestart": {
    "CronSchedule": "0 */4 * * *",
    "WarningIntervalsMinutes": [ 30, 15, 10, 5, 1 ],
    "CountdownSeconds": 3,
    "NotificationSound": "zUI.Success",
    "WaitForMatchEnd": true,
    "Extend": {
      "AllowedWindowMinutes": 30,
      "ExtendMinutes": 30,
      "RequiredPercentage": 80.0,
      "MaxExtendsPerCycle": 2,
      "MinPlayersToVote": 3
    },
    "Permissions": {
      "HardReset": "admin.permission.reset.hard",
      "SetSchedule": "admin.permission.reset.set",
      "ExtendReset": ""
    },
    "Webhook": {
      "Enabled": false,
      "WebhookUrl": "https://discord.com/api/webhooks/YOUR_WEBHOOK_URL",
      "BotUsername": "CS2 Server AutoRestart",
      "AvatarUrl": "https://i.imgur.com/8QfJq1M.png",
      "ServerNameOverride": "",
      "Events": {
        "NotifyOnWarning": true,
        "NotifyOnWaitingForMatchEnd": true,
        "NotifyOnHardReset": true,
        "NotifyOnExtendVote": true,
        "NotifyOnSetSchedule": true,
        "NotifyOnRestarting": true
      }
    }
  }
}
```

---

## Build & Installation

### Requirements
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SwiftlyS2](https://swiftlys2.net/) (v1.5.0+)

### Build
```bash
dotnet build -c Release
```

### Package
```bash
dotnet publish -c Release
```
The output zip archive will be generated at `build/zAutoRestart7GC.zip`. Extract it into your game server's `addons/swiftlys2/plugins/` directory.

---

## License
MIT License
