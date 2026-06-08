using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Instances;
using OsuPlayer.Playback;
using OsuPlayer.Presentation;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Utils;
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

        Services = new ServiceCollection()
            .ConfigureServices()
            .BuildServiceProvider();

        await EntryStartup.StartupAsync(Services);

        Services.GetRequiredService<LyricsInst>().ReloadLyricProvider();

        I18NUtil.LoadI18N();

        var controller = Services.GetRequiredService<ObservablePlayController>();
        controller.PlayStatusChanged += status =>
        {
            OsuPlayer.Core.SharedVm.Default.IsPlaying = status == OsuPlayer.Media.Audio.PlayStatus.Playing;
        };

        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        AppSettings.Default?.Dispose();
        LogManager.Shutdown();
    }
}
