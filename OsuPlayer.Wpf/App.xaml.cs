using Microsoft.Win32;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.I18N;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Utils;
using System;
using System.Windows;

namespace Milky.OsuPlayer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static bool UseDbMode => InstanceManage.GetInstance<OsuDbInst>().BeatmapDb != null;

        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;
            StartupConfig.Startup();

            InstanceManage.AddInstance(new UiMetadata());
            InstanceManage.AddInstance(new PlayerList());
            InstanceManage.AddInstance(new OsuDbInst());
            InstanceManage.AddInstance(new PlayersInst());
            InstanceManage.AddInstance(new LyricsInst());
            InstanceManage.AddInstance(new Updater());

            InstanceManage.GetInstance<LyricsInst>().ReloadLyricProvider();

            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        private static void OnCurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!e.IsTerminating) return;
            MessageBox.Show(string.Format("发生严重错误，即将退出。。。详情请查看error.log。{0}{1}", Environment.NewLine, (e.ExceptionObject as Exception)?.Message), "Osu Player", MessageBoxButton.OK, MessageBoxImage.Error);
            ConcurrentFile.AppendAllText("error.log", string.Format(@"===================={0}===================={1}{2}{3}{4}", DateTime.Now, Environment.NewLine, e.ExceptionObject, Environment.NewLine, Environment.NewLine));
            Environment.Exit(1);
        }

        public static bool? BrowseDb(out string chosedPath)
        {
            OpenFileDialog fbd = new OpenFileDialog
            {
                Title = @"请选择osu所在目录内的""osu!.db""",
                Filter = @"Beatmap Database|osu!.db"
            };
            var result = fbd.ShowDialog();
            chosedPath = fbd.FileName;
            return result;
        }
    }
}
