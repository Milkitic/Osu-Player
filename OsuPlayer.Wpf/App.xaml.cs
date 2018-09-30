using Microsoft.Win32;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Media.Lyric;
using Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Auto;
using Milkitic.OsuPlayer.Media.Music;
using Milkitic.OsuPlayer.Media.Storyboard;
using Milkitic.OsuPlayer;
using Newtonsoft.Json;
using osu.Shared.Serialization;
using osu_database_reader.BinaryFiles;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace Milkitic.OsuPlayer
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
        public static StoryboardProvider StoryboardProvider;

        public static readonly LyricProvider LyricProvider =
            new LyricProvider(new AutoSourceProvider(), LyricProvider.ProvideTypeEnum.Original);
        public static readonly PlayerControl PlayerControl = new PlayerControl();

        static App()
        {
            if (!LoadSettings())
                Environment.Exit(0);
            CreateDirectories();
            InitLocalDb();
            LoadOsuDb();
        }

        private static void InitLocalDb()
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

        private static void LoadOsuDb()
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

        /// <summary>
        /// 创建目录
        /// </summary>
        private static void CreateDirectories()
        {
            Type t = typeof(Domain);
            var infos = t.GetProperties();
            foreach (var item in infos)
            {
                if (!item.Name.EndsWith("Path")) continue;
                try
                {
                    string path = (string)item.GetValue(null, null);
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }
                catch (Exception)
                {
                    Console.WriteLine(@"未创建：" + item.Name);
                }
            }
        }
    }
}
