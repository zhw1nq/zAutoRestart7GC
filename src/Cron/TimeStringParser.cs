using System.Text.RegularExpressions;

namespace zAutoRestart7GC.Cron;

public static class TimeStringParser
{
  private static readonly Regex CombinedRegex = new(
      @"^(?:(?<hours>\d+)\s*h(?:ours?)?)?\s*(?:(?<minutes>\d+)\s*m(?:in(?:ute)?s?)?)?\s*(?:(?<seconds>\d+)\s*s(?:ec(?:ond)?s?)?)?$",
      RegexOptions.IgnoreCase | RegexOptions.Compiled);

  public static bool TryParse(string input, out TimeSpan result)
  {
    result = TimeSpan.Zero;
    if (string.IsNullOrWhiteSpace(input)) return false;

    var trimmed = input.Trim();

    // Direct plain integer fallback: treat as minutes (e.g. "30")
    if (int.TryParse(trimmed, out var plainMinutes))
    {
      if (plainMinutes <= 0) return false;
      result = TimeSpan.FromMinutes(plainMinutes);
      return true;
    }

    var match = CombinedRegex.Match(trimmed);
    if (!match.Success) return false;

    var hoursGroup = match.Groups["hours"];
    var minutesGroup = match.Groups["minutes"];
    var secondsGroup = match.Groups["seconds"];

    if (!hoursGroup.Success && !minutesGroup.Success && !secondsGroup.Success) return false;

    int hours = 0;
    int minutes = 0;
    int seconds = 0;

    if (hoursGroup.Success && !int.TryParse(hoursGroup.Value, out hours)) return false;
    if (minutesGroup.Success && !int.TryParse(minutesGroup.Value, out minutes)) return false;
    if (secondsGroup.Success && !int.TryParse(secondsGroup.Value, out seconds)) return false;

    if (hours == 0 && minutes == 0 && seconds == 0) return false;

    result = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
    return true;
  }
}
