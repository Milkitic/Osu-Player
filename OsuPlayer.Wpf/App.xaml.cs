using System;
using System.Windows;
using Milki.OsuPlayer.Common.Configuration;
using Milki.OsuPlayer.Common.Instances;
using Milki.OsuPlayer.Common.Scanning;
using Milki.OsuPlayer.Instances;
using Milki.OsuPlayer.Presentation.Interaction;
using Milki.OsuPlayer.Shared.Dependency;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.Windows;
using NLog;
#if !DEBUG
using Milky.OsuPlayer.Sentry;
#endif

namespace Milki.OsuPlayer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
#if !DEBUG
        public App()
        {
            LogManager.LoadConfiguration("NLog.config");
            SentryNLog.Init(LogManager.Configuration);
        }
#endif

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;
            DispatcherUnhandledException += Application_DispatcherUnhandledException;

            await EntryStartup.StartupAsync();

            var controller = new ObservablePlayController();
            controller.PlayList.Mode = AppSettings.Default.Play.PlayListMode;

            Service.TryAddInstance(controller);
            Service.TryAddInstance(new OsuDbInst());
            Service.TryAddInstance(new LyricsInst());
            Service.TryAddInstance(new UpdateInst());
            Service.TryAddInstance(new OsuFileScanner());

            Service.Get<LyricsInst>().ReloadLyricProvider();

            Execute.SetMainThreadContext();
            I18NUtil.LoadI18N();
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

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Fatal(e.Exception, "DispatcherUnhandledException");

            var exceptionWindow = new ExceptionWindow(e.Exception, true);
            var val = exceptionWindow.ShowDialog();
            e.Handled = val != true;
            if (val == true)
            {
                Environment.Exit(1);
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            AppSettings.Default?.Dispose();
            LogManager.Shutdown();
        }
    }
}
