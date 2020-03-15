using Microsoft.Win32;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Common.Scanning;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xaml;
using Milky.OsuPlayer.Control.Notification;

#if !DEBUG
using Sentry;
#endif

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
#if !DEBUG
            SentrySdk.Init("https://1fe13baa86284da5a0a70efa9750650e:fcbd468d43f94fb1b43af424517ec00b@sentry.io/1412154");
#endif
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;
            StartupConfig.Startup();

            var playerList = new  { PlayerMode = AppSettings.Default.Play.PlayMode };
            Services.TryAddInstance(playerList);
            Services.TryAddInstance(new OsuDbInst());
            //Services.TryAddInstance(new PlayersInst());
            Services.TryAddInstance(new LyricsInst());
            Services.TryAddInstance(new Updater());
            Services.TryAddInstance(new OsuFileScanner());

            Services.Get<LyricsInst>().ReloadLyricProvider();

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        private static void OnCurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
#if !DEBUG
                SentrySdk.CaptureException(ex);
#endif
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
#if !DEBUG
            SentrySdk.CaptureException(e.Exception);
#endif
            var exceptionWindow = new ExceptionWindow(e.Exception, true);
            var val = exceptionWindow.ShowDialog();
            e.Handled = val != true;
            if (val == true)
            {
                Environment.Exit(1);
            }
        }

        public static ObservableCollection<NotificationOption> NotificationList { get; set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            I18nUtil.LoadI18N();
        }
    }
}
