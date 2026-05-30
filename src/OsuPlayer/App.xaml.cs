using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Core.Configuration;
using Milky.OsuPlayer.Core.Instances;
using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.Core.Scanning;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.UserControls;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using NLog;

namespace Milky.OsuPlayer;

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

        await EntryStartup.StartupAsync();

        // 1. 初始化依赖注入容器
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        Services.GetRequiredService<LyricsInst>().ReloadLyricProvider();

        I18NUtil.LoadI18N();

        // 3. 从容器解析并启动 MainWindow
        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 注册核心后台服务 (Singletons)
        services.AddSingleton<IAppNotificationService, AppNotificationService>();
        services.AddSingleton<IPlayerDataStore, PlayerDataService>();
        services.AddSingleton<IPlayerDataService>(provider =>
            new NotifyingPlayerDataService(
                provider.GetRequiredService<IPlayerDataStore>(),
                provider.GetRequiredService<IAppNotificationService>()));

        services.AddSingleton(provider =>
        {
            var controller = new ObservablePlayController(provider.GetRequiredService<IPlayerDataStore>());
            controller.PlayList.Mode = AppSettings.Default.Play.PlayListMode;
            return controller;
        });
        services.AddSingleton<OsuDbInst>();
        services.AddSingleton<LyricsInst>();
        services.AddSingleton<UpdateInst>();
        services.AddSingleton<OsuFileScanner>();
        services.AddSingleton<IExportService, ExportService>();

        // 注册 ViewModels
        services.AddSingleton(_ => SharedVm.Default);
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<CollectionPageViewModel>();
        services.AddTransient<SearchPageViewModel>();
        services.AddSingleton<LyricWindowViewModel>();
        services.AddTransient<RecentPlayPageViewModel>();
        services.AddTransient<ExportPageViewModel>();
        services.AddTransient<PlayListControlVm>();

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