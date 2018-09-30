using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Milkitic.OsuLib;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer;
using Milkitic.OsuPlayer.Utils;
using osu_database_reader.Components.Beatmaps;
using Path = System.IO.Path;

namespace Milkitic.OsuPlayer.Pages
{
    /// <summary>
    /// ExportPage.xaml 的交互逻辑
    /// </summary>
    public partial class ExportPage : Page
    {
        private readonly MainWindow _mainWindow;
        public static readonly ConcurrentQueue<BeatmapEntry> TaskQueue = new ConcurrentQueue<BeatmapEntry>();
        public static Task ExportTask;
        public static bool Overlap = true;

        public ExportPage(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
            Update();
        }

        private void Update()
        {
            var maps = (List<MapInfo>)DbOperator.GetExportedMaps();
            ExportList.DataContext = App.Beatmaps.GetMapListFromDb(maps).Transform(false).ToList();
        }

        private void ExportList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void ItemDelete_Click(object sender, RoutedEventArgs e)
        {

        }

        public static void QueueEntries(IEnumerable<BeatmapEntry> entries)
        {
            foreach (var entry in entries)
                TaskQueue.Enqueue(entry);

            StartTask();
        }

        public static void QueueEntry(BeatmapEntry entry)
        {
            TaskQueue.Enqueue(entry);
            StartTask();
        }

        private static void StartTask()
        {
            if (ExportTask != null && !ExportTask.IsCanceled && !ExportTask.IsCompleted)
                return;
            ExportTask = Task.Run(() =>
            {
                while (!TaskQueue.IsEmpty)
                {
                    if (!TaskQueue.TryDequeue(out var entry))
                        continue;
                    CopyFile(entry);
                }
            });
        }

        private static void CopyFile(BeatmapEntry entry)
        {
            string folder = Path.Combine(Domain.OsuSongPath, entry.FolderName);
            FileInfo mp3File = new FileInfo(Path.Combine(folder, entry.AudioFileName));
            var osufile = new OsuFile(Path.Combine(folder, entry.BeatmapFileName));
            FileInfo bgFile = new FileInfo(Path.Combine(folder, osufile.Events.BackgroundInfo.Filename));

            var artist = MetaSelect.GetUnicode(entry.Artist, entry.ArtistUnicode);
            var title = MetaSelect.GetUnicode(entry.Title, entry.TitleUnicode);
            var escapedMp3 = Escape($"{artist} - {title}");
            var escapedBg = Escape($"{artist} - {title}({entry.Creator})[{entry.Version}]");
            string validMp3Name = ValidateFilename(escapedMp3, Domain.MusicPath, mp3File.Extension);
            string validBgName = ValidateFilename(escapedBg, Domain.BackgroundPath, bgFile.Extension);

            if (mp3File.Exists)
                Export(mp3File, Domain.MusicPath, validMp3Name);
            if (bgFile.Exists)
                Export(bgFile, Domain.BackgroundPath, validBgName);
            if (mp3File.Exists || bgFile.Exists)
                DbOperator.AddMapExport(entry.GetIdentity(), validMp3Name + mp3File.Extension);
        }

        private static void Export(FileInfo originFile, string outputDir, string outputFile)
        {
            var path = Path.Combine(outputDir, outputFile + originFile.Extension);
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            if (Overlap && File.Exists(path))
                File.Delete(path);
            File.Copy(originFile.FullName, path);
            UpdateFileTime(path);
        }

        private static string ValidateFilename(string escaped, string dirPath, string ext)
        {
            if (Overlap) return escaped;
            var validName = escaped;
            int i = 1;
            if (string.IsNullOrEmpty(validName.Trim()))
                validName = "未知歌手 - 未知标题";
            while (File.Exists(Path.Combine(dirPath, validName + ext)))
            {
                validName = escaped + $" ({i})";
                i++;
            }

            return validName;
        }

        private static void UpdateFileTime(string mp3Path)
        {
            var time = DateTime.Now;
            _ = new FileInfo(mp3Path)
            {
                CreationTime = time,
                LastAccessTime = time,
                LastWriteTime = time
            };
        }

        private static string Escape(string source)
        {
            return source.Replace(@"\", "").Replace(@"/", "").Replace(@":", "").Replace(@"*", "").Replace(@"?", "")
                .Replace("\"", "").Replace(@"<", "").Replace(@">", "").Replace(@"|", "");
        }
    }
}
