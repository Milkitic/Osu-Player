using Microsoft.Win32;
using Milkitic.OsuPlayer;
using Milkitic.OsuPlayer.Control;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.L18N;
using Milkitic.OsuPlayer.Media.Lyric;
using Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Auto;
using Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Base;
using Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Kugou;
using Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Netease;
using Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.QQMusic;
using Milkitic.OsuPlayer.Media.Music;
using Milkitic.OsuPlayer.Media.Storyboard;
using Milkitic.OsuPlayer.Utils;
using Newtonsoft.Json;
using osu.Shared.Serialization;
using osu_database_reader.BinaryFiles;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Milkitic.OsuPlayer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static Config Config { get; set; }
        public static UiMetadata UiMetadata { get; set; } = new UiMetadata();
        public static bool UseDbMode => Config.General.DbPath != null;

        public static Lazy<OsuDb> BeatmapDb { get; set; } = new Lazy<OsuDb>(ReadDb);

        public static List<BeatmapEntry> Beatmaps => BeatmapDb.Value?.Beatmaps;

        public static MusicPlayer MusicPlayer;
        public static HitsoundPlayer HitsoundPlayer;
        public static StoryboardProvider StoryboardProvider;

        public static LyricProvider LyricProvider;
        public static readonly PlayerList PlayerList = new PlayerList();
        public static readonly Updater Updater = new Updater();

        [STAThread]
        public static void Main()
        {
            try
            {
                //var dllDirectory = Path.Combine(Domain.CurrentPath, "bin");
                //if (!Directory.Exists(dllDirectory))
                //    Directory.CreateDirectory(dllDirectory);
                //foreach (var item in new DirectoryInfo(Domain.CurrentPath).EnumerateFiles())
                //{
                //    if (item.Extension.ToLower() == ".dll")
                //    {
                //        var newFile = Path.Combine(dllDirectory, item.Name);
                //        if (File.Exists(newFile))
                //            File.Delete(newFile);
                //        item.MoveTo(newFile);
                //    }
                //}
                //AppDomain.CurrentDomain.AppendPrivatePath(@"bin");

                if (!LoadConfig())
                    Environment.Exit(0);
                CreateDirectories();
                InitLocalDb();
                LoadOsuDb();
                SaveConfig();
                ReloadLyricProvider();
                RedirectHandler.Redirect();
            }
            catch (Exception e)
            {
                MessageBox.Show($"发生严重错误，即将退出。。。详情请查看error.log。{Environment.NewLine}{e.Message}", "Osu Player",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                File.AppendAllText("error.log",
                    $@"{DateTime.Now}===================={Environment.NewLine}{e}{Environment.NewLine}");
                Environment.Exit(1);
            }

            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        public static void ReloadLyricProvider()
        {
            bool strict = Config.Lyric.StrictMode;
            SourceProviderBase provider;
            switch (Config.Lyric.LyricSource)
            {
                case LyricSource.Auto:
                    provider = new AutoSourceProvider(strict);
                    break;
                case LyricSource.Netease:
                    provider = new NeteaseSourceProvider(strict);
                    break;
                case LyricSource.Kugou:
                    provider = new KugouSourceProvider(strict);
                    break;
                case LyricSource.QqMusic:
                    provider = new QqMusicSourceProvider(strict);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            LyricProvider = new LyricProvider(provider, LyricProvider.ProvideTypeEnum.Original, strict);
        }

        private static void InitLocalDb()
        {
            var defCol = DbOperator.GetCollections().Where(k => k.Locked);
            if (!defCol.Any()) DbOperator.AddCollection("最喜爱的", true);
        }

        private static bool LoadConfig()
        {
            var file = Domain.ConfigFile;
            if (!File.Exists(file))
            {
                CreateConfig();
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
                        "Osu Player", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        CreateConfig();
                    }
                    else
                        return false;
                }
            }

            return true;
        }

        private static void LoadOsuDb()
        {
            string dbPath = Config.General.DbPath;
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
                    var result = BrowserDb(out var chosedPath);
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
            }

            if (dbPath == null) return;
            Config.General.DbPath = dbPath;
        }

        public static bool? BrowserDb(out string chosedPath)
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

        private static void CreateConfig()
        {
            Config = new Config();
            File.WriteAllText(Domain.ConfigFile, JsonConvert.SerializeObject(Config));
        }

        public static void SaveConfig()
        {
            File.WriteAllText(Domain.ConfigFile, ConvertJsonString(JsonConvert.SerializeObject(Config)));
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

        private static string ConvertJsonString(string str)
        {
            //格式化json字符串
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(str);
            JsonTextReader jtr = new JsonTextReader(tr);
            object obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                return str;
            }
        }

        public static OsuDb ReadDb()
        {
            if (string.IsNullOrEmpty(Config.General.DbPath))
                return null;
            //var copied = Path.Combine(Domain.CurrentPath, "osu.db");
            //File.Copy(Config.General.DbPath, copied, true);
            var db = new OsuDb();
            using (FileStream fs = new FileStream(Config.General.DbPath, FileMode.Open))
            {
                db.ReadFromStream(new SerializationReader(fs));
            }
            return db;
        }
    }
}
