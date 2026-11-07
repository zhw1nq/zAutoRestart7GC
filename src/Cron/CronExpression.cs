using System.Text.RegularExpressions;

namespace zAutoRestart7GC.Cron;

/// <summary>
/// A zero-dependency 5-field cron parser supporting:
/// Minute (0-59), Hour (0-23), Day of Month (1-31), Month (1-12), Day of Week (0-6, 0=Sunday).
/// Supports *, */step, lists (1,2,3), and ranges (1-5).
/// </summary>
public class CronExpression
{
  private readonly HashSet<int> _minutes;
  private readonly HashSet<int> _hours;
  private readonly HashSet<int> _daysOfMonth;
  private readonly HashSet<int> _months;
  private readonly HashSet<int> _daysOfWeek;

  public string Expression { get; }

  public CronExpression(string expression)
  {
    Expression = expression?.Trim() ?? throw new ArgumentNullException(nameof(expression));
    var parts = Regex.Split(Expression, @"\s+");
    if (parts.Length != 5)
    {
      throw new ArgumentException($"Cron expression '{expression}' must contain exactly 5 fields (minute, hour, dom, month, dow).");
    }

    _minutes = ParseField(parts[0], 0, 59);
    _hours = ParseField(parts[1], 0, 23);
    _daysOfMonth = ParseField(parts[2], 1, 31);
    _months = ParseField(parts[3], 1, 12);
    _daysOfWeek = ParseDayOfWeekField(parts[4]);
  }


  public static bool TryParse(string expression, out CronExpression? result)
  {
    try
    {
      result = new CronExpression(expression);
      return true;
    }
    catch
    {
      result = null;
      return false;
    }
  }

  public DateTime? GetNextOccurrence(DateTime from)
  {
    // Start from the next minute, zeroing out seconds and milliseconds
    var current = new DateTime(from.Year, from.Month, from.Day, from.Hour, from.Minute, 0, from.Kind).AddMinutes(1);

    // Search up to 366 days in advance
    var limit = from.AddYears(1);

    while (current <= limit)
    {
      if (!_months.Contains(current.Month))
      {
        current = new DateTime(current.Year, current.Month, 1, 0, 0, 0, current.Kind).AddMonths(1);
        continue;
      }

      if (!_daysOfMonth.Contains(current.Day) || !_daysOfWeek.Contains((int)current.DayOfWeek))
      {
        current = new DateTime(current.Year, current.Month, current.Day, 0, 0, 0, current.Kind).AddDays(1);
        continue;
      }

      if (!_hours.Contains(current.Hour))
      {
        current = new DateTime(current.Year, current.Month, current.Day, current.Hour, 0, 0, current.Kind).AddHours(1);
        continue;
      }

      if (!_minutes.Contains(current.Minute))
      {
        current = current.AddMinutes(1);
        continue;
      }

      return current;
    }

    return null;
  }

  private static HashSet<int> ParseDayOfWeekField(string field)
  {
    var set = ParseField(field, 0, 7);
    if (set.Contains(7))
    {
      set.Remove(7);
      set.Add(0); // 7 is also Sunday in some cron notations
    }
    return set;
  }

  private static HashSet<int> ParseField(string field, int min, int max)
  {
    var result = new HashSet<int>();

    var subParts = field.Split(',');
    foreach (var subPart in subParts)
    {
      var token = subPart.Trim();
      if (token == "*")
      {
        for (int i = min; i <= max; i++) result.Add(i);
        continue;
      }

      if (token.StartsWith("*/"))
      {
        if (!int.TryParse(token[2..], out var step) || step <= 0)
        {
          throw new FormatException($"Invalid step in cron token: '{token}'");
        }
        for (int i = min; i <= max; i += step) result.Add(i);
        continue;
      }

      if (token.Contains('-'))
      {
        var range = token.Split('-');
        if (range.Length != 2 || !int.TryParse(range[0], out var start) || !int.TryParse(range[1], out var end))
        {
          throw new FormatException($"Invalid range in cron token: '{token}'");
        }
        for (int i = start; i <= end; i++)
        {
          if (i >= min && i <= max) result.Add(i);
        }
        continue;
      }

      if (int.TryParse(token, out var val))
      {
        if (val < min || val > max)
        {
          throw new ArgumentOutOfRangeException(nameof(field), $"Value {val} out of range ({min}-{max}) in cron token '{token}'");
        }
        result.Add(val);
        continue;
      }

      throw new FormatException($"Unrecognized cron token: '{token}'");
    }

    return result;
  }
}
