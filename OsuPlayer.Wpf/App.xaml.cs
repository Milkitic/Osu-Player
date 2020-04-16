using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Scanning;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.Windows;
using NLog;
using System;
using System.Windows;

namespace Milky.OsuPlayer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;

            EntryStartup.Startup();

            var controller = new ObservablePlayController();
            controller.PlayList.Mode = AppSettings.Default.Play.PlayListMode;

            Service.TryAddInstance(controller);
            Service.TryAddInstance(new OsuDbInst());
            Service.TryAddInstance(new LyricsInst());
            Service.TryAddInstance(new UpdateInst());
            Service.TryAddInstance(new OsuFileScanner());

            Service.Get<LyricsInst>().ReloadLyricProvider();

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

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
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

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Execute.SetMainThreadContext();
            I18NUtil.LoadI18N();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            AppSettings.Default?.Dispose();
            LogManager.Shutdown();
        }
    }
}
