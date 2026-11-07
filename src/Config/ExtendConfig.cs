namespace zAutoRestart7GC.Config;

public class ExtendConfig
{
  public int AllowedWindowMinutes { get; set; } = 30;
  public int ExtendMinutes { get; set; } = 30;
  public double RequiredPercentage { get; set; } = 80.0;
  public int MaxExtendsPerCycle { get; set; } = 2;
  public int MinPlayersToVote { get; set; } = 3;
}

