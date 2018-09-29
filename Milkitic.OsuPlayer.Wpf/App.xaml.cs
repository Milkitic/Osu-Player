using Microsoft.Win32;
using Milkitic.OsuPlayer.Wpf.Data;
using Milkitic.OsuPlayer.Wpf.LyricExtension;
using Milkitic.OsuPlayer.Wpf.LyricExtension.SourcePrivoder.Auto;
using Milkitic.OsuPlayer.Wpf.Storyboard;
using Milkitic.OsuPlayer.Wpf.Utils;
using Newtonsoft.Json;
using osu.Shared.Serialization;
using osu_database_reader.BinaryFiles;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Milkitic.OsuPlayer.Wpf
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static Config Config { get; set; }

        public static Lazy<OsuDb> BeatmapDb { get; set; } = new Lazy<OsuDb>(() =>
        {
            if (string.IsNullOrEmpty(Config.DbPath))
                return null;
            var db = new OsuDb();
            db.ReadFromStream(new SerializationReader(new FileStream(Config.DbPath, FileMode.Open)));
            return db;
        });

        public static List<BeatmapEntry> Beatmaps => BeatmapDb.Value?.Beatmaps;

        public static MusicPlayer MusicPlayer;
        public static HitsoundPlayer HitsoundPlayer;
        public static LyricProvider LyricProvider;
        public static StoryboardProvider StoryboardProvider;

        static App()
        {
            InitDb();
            LyricProvider = new LyricProvider(new AutoSourceProvider(), LyricProvider.ProvideTypeEnum.Original);

            if (!LoadSettings()) return;
            LoadDb();
        }

        private static void InitDb()
        {
            var defCol = DbOperator.GetCollections().Where(k => k.Locked);
            if (!defCol.Any()) DbOperator.AddCollection("最喜爱的", true);
        }

        private static bool LoadSettings()
        {
            var file = Domain.ConfigFile;
            if (!File.Exists(file))
            {
                CreateConfig(file);
            }
            else
            {
                try
                {
                    Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(file));
                }
                catch (JsonException e)
                {
                    var result = MessageBox.Show(@"载入配置文件时失败，用默认配置覆盖继续打开吗？\r\n" + e.Message,
                        AppDomain.CurrentDomain.FriendlyName, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        CreateConfig(file);
                    }
                    else
                        return false;
                }
            }

            return true;
        }

        private static void LoadDb()
        {
            string dbPath = Config.DbPath;
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
                    string chosedPath;
                    OpenFileDialog fbd = new OpenFileDialog
                    {
                        Title = @"请选择osu所在目录内的""osu!.db""",
                        Filter = @"Beatmap Database|osu!.db"
                    };
                    var result = fbd.ShowDialog();
                    if (result.HasValue && result.Value)
                        chosedPath = fbd.FileName;
                    else
                    {
                        MessageBox.Show(@"你尚未初始化osu!db，因此部分功能将不可用。", typeof(App).Name, MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!File.Exists(chosedPath))
                    {
                        MessageBox.Show(@"指定文件不存在。", typeof(App).Name, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    dbPath = chosedPath;
                }
            }

            if (dbPath == null) return;
            Config.DbPath = dbPath;

        }

        private static void CreateConfig(string file)
        {
            Config = new Config();
            File.WriteAllText(file, JsonConvert.SerializeObject(Config));
        }
    }
}
