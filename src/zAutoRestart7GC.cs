using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;
using zAutoRestart7GC.Commands;
using zAutoRestart7GC.Config;
using zAutoRestart7GC.Listeners;
using zAutoRestart7GC.Services;

namespace zAutoRestart7GC;

[PluginMetadata(
    Id = "zAutoRestart7GC",
    Version = "1.0.0",
    Name = "zAutoRestart7GC",
    Author = "vhming",
    Description = "Advanced server auto-restart plugin with crontab, HTML/chat warnings, 80% vote extend, and Discord webhook.")]
public partial class zAutoRestart7GC : BasePlugin
{
  private ServiceProvider? _serviceProvider;
  private IRestartManager? _restartManager;
  private PlayerEventListener? _playerEventListener;
  private MatchEventListener? _matchEventListener;

  public zAutoRestart7GC(ISwiftlyCore core) : base(core)
  {
  }

  public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
  {
  }

  public override void UseSharedInterface(IInterfaceManager interfaceManager)
  {
  }

  public override void Load(bool hotReload)
  {
    Core.Logger.LogInformation("Loading zAutoRestart7GC (hotReload: {HotReload})...", hotReload);

    // 1. Initialize configuration with bundled template
    Core.Configuration
        .InitializeWithTemplate("config.jsonc", "template.jsonc")
        .Configure(builder =>
        {
          builder.AddJsonFile("config.jsonc", optional: false, reloadOnChange: true);
        });

    // 2. Setup Dependency Injection
    var services = new ServiceCollection();
    services.AddSwiftly(Core);
    services.AddOptionsWithValidateOnStart<AutoRestartConfig>().BindConfiguration("AutoRestart");

    // Services
    services.AddSingleton<ISoundService, SoundService>();
    services.AddSingleton<IMessageFormatter, MessageFormatter>();
    services.AddSingleton<IWebhookService, WebhookService>();
    services.AddSingleton<IVoteExtendManager, VoteExtendManager>();
    services.AddSingleton<IRestartManager, RestartManager>();

    // Event listeners
    services.AddSingleton<PlayerEventListener>();
    services.AddSingleton<MatchEventListener>();

    // Commands
    services.AddSingleton<AdminResetCommands>();
    services.AddSingleton<PlayerResetCommands>();

    _serviceProvider = services.BuildServiceProvider();

    // 3. Register attribute-based command handlers
    Core.Registrator.Register(_serviceProvider.GetRequiredService<AdminResetCommands>());
    Core.Registrator.Register(_serviceProvider.GetRequiredService<PlayerResetCommands>());

    // 4. Instantiate listeners
    _playerEventListener = _serviceProvider.GetRequiredService<PlayerEventListener>();
    _matchEventListener = _serviceProvider.GetRequiredService<MatchEventListener>();

    // 5. Start auto-restart lifecycle
    _restartManager = _serviceProvider.GetRequiredService<IRestartManager>();
    _restartManager.Initialize();

    Core.Logger.LogInformation("zAutoRestart7GC loaded successfully.");
  }

  public override void Unload()
  {
    Core.Logger.LogInformation("Unloading zAutoRestart7GC...");

    _restartManager?.Shutdown();
    _playerEventListener?.Dispose();
    _matchEventListener?.Dispose();

    _serviceProvider?.Dispose();
    _serviceProvider = null;

    Core.Logger.LogInformation("zAutoRestart7GC unloaded successfully.");
  }
}
