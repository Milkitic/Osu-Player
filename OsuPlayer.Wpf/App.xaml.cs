using Microsoft.Win32;
using Milky.OsuPlayer;
using Milky.OsuPlayer.Control;
using Newtonsoft.Json;
using osu.Shared.Serialization;
using osu_database_reader.BinaryFiles;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.L18N;
using Milky.OsuPlayer.Media.Lyric;
using Milky.OsuPlayer.Media.Lyric.SourcePrivoder.Auto;
using Milky.OsuPlayer.Media.Lyric.SourcePrivoder.Base;
using Milky.OsuPlayer.Media.Lyric.SourcePrivoder.Kugou;
using Milky.OsuPlayer.Media.Lyric.SourcePrivoder.Netease;
using Milky.OsuPlayer.Media.Lyric.SourcePrivoder.QQMusic;
using Milky.OsuPlayer.Media.Music;
using Milky.OsuPlayer.Media.Storyboard;
using Milky.OsuPlayer.Utils;

namespace Milky.OsuPlayer
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
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;
            //Migrate();

            //AppDomain.CurrentDomain.AppendPrivatePath(@"bin");

            if (!LoadConfig())
                Environment.Exit(0);
            CreateDirectories();
            InitLocalDb();
            LoadOsuDb();
            SaveConfig();
            ReloadLyricProvider();
            RedirectHandler.Redirect();
            SetAlignment();
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        public static void SetAlignment()
        {
            //获取系统是以Left-handed（true）还是Right-handed（false）
            var ifLeft = SystemParameters.MenuDropAlignment;

            if (ifLeft)
            {
                // change to false
                var t = typeof(SystemParameters);
                var field = t.GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
                field?.SetValue(null, false);

                ifLeft = SystemParameters.MenuDropAlignment;
            }
        }

        private static void Migrate()
        {
            var path = Path.Combine(Domain.CurrentPath, "migrate.bat");
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = Process.GetCurrentProcess().Id.ToString(),
                    CreateNoWindow = true
                });
                Environment.Exit(0);
            }
        }

        private static void OnCurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!e.IsTerminating) return;
            MessageBox.Show(string.Format("发生严重错误，即将退出。。。详情请查看error.log。{0}{1}", Environment.NewLine, (e.ExceptionObject as Exception)?.Message), "Osu Player", MessageBoxButton.OK, MessageBoxImage.Error);
            File.AppendAllText("error.log", string.Format(@"===================={0}===================={1}{2}{3}{4}", DateTime.Now, Environment.NewLine, e.ExceptionObject, Environment.NewLine, Environment.NewLine));
            Environment.Exit(1);
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

            LyricProvider = new LyricProvider(provider, LyricProvideType.Original, strict);
        }

        private static void InitLocalDb()
        {
            var defCol = DbOperate.GetCollections().Where(k => k.Locked);
            if (!defCol.Any()) DbOperate.AddCollection("最喜爱的", true);
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
