using Milkitic.OsuPlayer.LyricExtension;
using Milkitic.OsuPlayer.LyricExtension.SourcePrivoder.Auto;
using Milkitic.OsuPlayer.Storyboard;
using Milkitic.OsuPlayer.Utils;
using Milkitic.OsuPlayer.Winforms;
using Newtonsoft.Json;
using osu.Shared.Serialization;
using osu_database_reader.BinaryFiles;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Milkitic.OsuPlayer
{
    static class Core
    {
        public static Config Config { get; set; }
        public static OsuDb BeatmapDb { get; set; }
        public static List<BeatmapEntry> Beatmaps => BeatmapDb.Beatmaps;

        public static MusicPlayer MusicPlayer;
        public static HitsoundPlayer HitsoundPlayer;
        public static LyricProvider LyricProvider;
        public static StoryboardProvider StoryboardProvider;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!LoadSettings()) return;
            LoadDb();

            LyricProvider = new LyricProvider(new AutoSourceProvider(), LyricProvider.ProvideTypeEnum.Original);

            Application.Run(new MainForm());
            SaveConfig(Domain.ConfigFile);
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
                        AppDomain.CurrentDomain.FriendlyName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
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
                    if (fbd.ShowDialog() == DialogResult.OK)
                        chosedPath = fbd.FileName;
                    else
                    {
                        MessageBox.Show(@"你尚未初始化osu!db，因此部分功能将不可用。", typeof(Core).Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (!File.Exists(chosedPath))
                    {
                        MessageBox.Show(@"指定文件不存在。", typeof(Core).Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    dbPath = chosedPath;
                }
            }

            if (dbPath == null) return;
            Config.DbPath = dbPath;
            BeatmapDb = new OsuDb();
            BeatmapDb.ReadFromStream(new SerializationReader(new FileStream(dbPath, FileMode.Open)));
        }

        private static void SaveConfig(string file)
        {
            File.WriteAllText(file, JsonConvert.SerializeObject(Config));
        }

        private static void CreateConfig(string file)
        {
            Config = new Config();
            File.WriteAllText(file, JsonConvert.SerializeObject(Config));
        }
    }
}
