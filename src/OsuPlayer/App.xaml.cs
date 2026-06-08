using System;
using System.Windows;
using KeyAsio.Core.Audio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Instances;
using OsuPlayer.Core.Scanning;
using OsuPlayer.Core.Services;
using OsuPlayer.Data;
using OsuPlayer.Instances;
using OsuPlayer.Media.Audio;
using OsuPlayer.Media.Audio.Coordination;
using OsuPlayer.Media.Audio.Playlist;
using OsuPlayer.Pages;
using OsuPlayer.Presentation;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Services;
using OsuPlayer.Shared;
using OsuPlayer.UserControls;
using OsuPlayer.Utils;
using OsuPlayer.ViewModels;
using OsuPlayer.Windows;

namespace OsuPlayer;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; }

    [STAThread]
    public static void Main()
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    private static void OnCurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Fatal(ex, "UnhandledException");

            var exceptionWindow = new ExceptionWindow(ex, false);
            exceptionWindow.ShowDialog();
        }

        if (!e.IsTerminating)
        {
            return;
        }

        Environment.Exit(1);
    }

    private void Application_DispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var logger = LogManager.GetCurrentClassLogger();
        logger.Error(e.Exception, "DispatcherUnhandledException");

        var exceptionWindow = new ExceptionWindow(e.Exception, true);
        var val = exceptionWindow.ShowDialog();
        e.Handled = val != true;
        if (val == true)
        {
            Environment.Exit(1);
        }
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        Execute.SetMainThreadContext();
        AnimationOptions.DisableAnimations = () => AppSettings.Default?.Interface?.MinimalMode == true;

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        await EntryStartup.StartupAsync(Services);

        Services.GetRequiredService<LyricsInst>().ReloadLyricProvider();

        I18NUtil.LoadI18N();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            loggingBuilder.AddNLog();
        });
        services.AddAudioModule();

        services.AddTransient<OsuPlayerDbContext>(provider =>
        {
            var options = new DbContextOptionsBuilder<OsuPlayerDbContext>()
                .UseSqlite(OsuPlayerDbContext.DefaultConnectionString)
                .Options;
            return new OsuPlayerDbContext(options, provider.GetRequiredService<ILogger<OsuPlayerDbContext>>());
        });
        services.AddSingleton<Func<OsuPlayerDbContext>>(provider => () => provider.GetRequiredService<OsuPlayerDbContext>());

        services.AddSingleton<IBeatmapThumbnailService, WpfBeatmapThumbnailService>();
        services.AddSingleton<IMapModelConverter, MapModelConverter>();
        services.AddSingleton<BeatmapLoader>();

        services.AddSingleton<IAppNotificationService, AppNotificationService>();
        services.AddSingleton<IBeatmapDifficultyPicker, DialogBeatmapDifficultyPicker>();
        services.AddTransient<INavigationService, FrameNavigationService>();
        services.AddSingleton<IUiThreadDispatcher>(_ => Execute.UiThreadDispatcher);
        services.AddSingleton<IPlayerDataStore, PlayerDataService>();
        services.AddSingleton<IPlayerDataService>(provider =>
            new NotifyingPlayerDataService(
                provider.GetRequiredService<IPlayerDataStore>(),
                provider.GetRequiredService<IAppNotificationService>()));

        services.AddSingleton<PlayerEventBus>();
        services.AddSingleton<PlayList>();
        services.AddSingleton<PlayerSessionService>();

        services.AddSingleton(provider =>
        {
            var controller = new ObservablePlayController(
                provider.GetRequiredService<IPlaybackEngine>(),
                provider.GetRequiredService<PlayerEventBus>(),
                provider.GetRequiredService<PlayList>(),
                provider.GetRequiredService<PlayerSessionService>(),
                provider.GetRequiredService<ILogger<ObservablePlayController>>(),
                ex => provider.GetRequiredService<IAppNotificationService>().Push(ex.Message, "Audio Device Error"));
            controller.PlayList.Mode = AppSettings.Default.Play.PlayListMode;
            return controller;
        });
        services.AddSingleton<OsuDbInst>();
        services.AddSingleton<LyricsInst>();
        services.AddSingleton<UpdateInst>();
        services.AddSingleton<OsuFileScanner>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IBeatmapActionService, BeatmapActionService>();

        // 注册 ViewModels
        services.AddSingleton(_ => SharedVm.Default);
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<CollectionPageViewModel>();
        services.AddTransient<SearchPageViewModel>();
        services.AddSingleton<LyricWindowViewModel>();
        services.AddTransient<RecentPlayPageViewModel>();
        services.AddTransient<ExportPageViewModel>();
        services.AddTransient<PlayListControlVm>();
        services.AddTransient<InterfacePageViewModel>();
        services.AddTransient<AboutPageViewModel>();
        services.AddTransient<GeneralPageViewModel>();
        services.AddTransient<PlayPageViewModel>();

        // 注册 Windows / Pages
        services.AddSingleton<MainWindow>();
        services.AddSingleton<LyricWindow>();
        services.AddTransient<ConfigWindow>();
        services.AddTransient<MiniWindow>();
        services.AddTransient<CollectionPage>();
        services.AddTransient<SearchPage>();
        services.AddTransient<RecentPlayPage>();
        services.AddTransient<ExportPage>();
        services.AddTransient<StoryboardPage>();

        // 注册 Settings Pages
        services.AddTransient<Pages.Settings.AboutPage>();
        services.AddTransient<Pages.Settings.ExportPage>();
        services.AddTransient<Pages.Settings.GeneralPage>();
        services.AddTransient<Pages.Settings.HotKeyPage>();
        services.AddTransient<Pages.Settings.InterfacePage>();
        services.AddTransient<Pages.Settings.LyricPage>();
        services.AddTransient<Pages.Settings.PlayPage>();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        AppSettings.Default?.Dispose();
        LogManager.Shutdown();
    }
}
