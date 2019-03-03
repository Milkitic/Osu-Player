using Microsoft.Win32;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.I18N;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Lyric;
using Milky.OsuPlayer.Media.Lyric.SourceProvider;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.Auto;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.Kugou;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.Netease;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.QQMusic;
using Milky.OsuPlayer.Utils;
using Milky.WpfApi;
using Newtonsoft.Json;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Milky.OsuPlayer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static bool UseDbMode => PlayerConfig.Current.General.DbPath != null;

        [STAThread]
        public static void Main()
        {
            //AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;
            StartupConfig.Startup();

            InstanceManage.AddInstance(new UiMetadata());
            InstanceManage.AddInstance(new PlayerList());
            InstanceManage.AddInstance(new OsuDbInst());
            InstanceManage.AddInstance(new PlayersInst());
            InstanceManage.AddInstance(new LyricsInst());
            InstanceManage.AddInstance(new Updater());

            InstanceManage.GetInstance<LyricsInst>().ReloadLyricProvider();

            LoadOsuDbAsync().Wait();

            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        private static void OnCurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!e.IsTerminating) return;
            MessageBox.Show(string.Format("发生严重错误，即将退出。。。详情请查看error.log。{0}{1}", Environment.NewLine, (e.ExceptionObject as Exception)?.Message), "Osu Player", MessageBoxButton.OK, MessageBoxImage.Error);
            File.AppendAllText("error.log", string.Format(@"===================={0}===================={1}{2}{3}{4}", DateTime.Now, Environment.NewLine, e.ExceptionObject, Environment.NewLine, Environment.NewLine));
            Environment.Exit(1);
        }

        private static async Task LoadOsuDbAsync()
        {
            string dbPath = PlayerConfig.Current.General.DbPath;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
            {
                var osuProcess = Process.GetProcesses().Where(x => x.ProcessName == "osu!").ToArray();
                if (osuProcess.Length == 1)
                {
                    var di = new FileInfo(osuProcess[0].MainModule.FileName).Directory;
                    if (di != null && di.Exists)
                        dbPath = Path.Combine(di.FullName, "osu!.db");
                }

                if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
                {
                    var result = BrowseDb(out var chosedPath);
                    if (!result.HasValue || !result.Value)
                    {
                        MessageBox.Show(@"你尚未初始化osu!db，因此部分功能将不可用。", "Osu Player", MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    if (!File.Exists(chosedPath))
                    {
                        MessageBox.Show(@"指定文件不存在。", "Osu Player", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    dbPath = chosedPath;
                }

                //if (dbPath == null) return;
                PlayerConfig.Current.General.DbPath = dbPath;
            }

            await InstanceManage.GetInstance<OsuDbInst>().LoadNewDbAsync(dbPath);
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

    public static class StartupConfig
    {
        public static void Startup()
        {
            if (!LoadConfig())
                Environment.Exit(0);

            InitLocalDb();

            RedirectEventHandle.Redirect();
            StyleUtilities.SetAlignment();
        }

        private static bool LoadConfig()
        {
            var file = Domain.ConfigFile;
            if (!File.Exists(file))
            {
                PlayerConfig.CreateNewConfig();
            }
            else
            {
                try
                {
                    PlayerConfig.Load(JsonConvert.DeserializeObject<PlayerConfig>(File.ReadAllText(file)));
                }
                catch (JsonException e)
                {
                    var result = MessageBox.Show(@"载入配置文件时失败，用默认配置覆盖继续打开吗？\r\n" + e.Message,
                        "Osu Player", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        PlayerConfig.CreateNewConfig();
                    }
                    else
                        return false;
                }
            }

            return true;
        }

        private static void InitLocalDb()
        {
            var defCol = DbOperate.GetCollections().Where(k => k.Locked);
            if (!defCol.Any()) DbOperate.AddCollection("最喜爱的", true);
        }
    }
}
