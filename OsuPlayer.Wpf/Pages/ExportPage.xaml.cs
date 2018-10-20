using Milkitic.OsuLib;
using Milkitic.OsuPlayer;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Utils;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private IEnumerable<BeatmapEntry> _entries;

        public ExportPage(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
            UpdateList();
        }

        private void UpdateList()
        {
            var maps = (List<MapInfo>)DbOperator.GetExportedMaps();
            List<(MapIdentity MapIdentity, string path, string time, string size)> list =
                new List<(MapIdentity, string, string, string)>();
            foreach (var map in maps)
            {
                var fi = new FileInfo(map.ExportFile);
                list.Add(!fi.Exists
                    ? (map.GetIdentity(), map.ExportFile, "已从目录移除", "已从目录移除")
                    : (map.GetIdentity(), map.ExportFile, fi.CreationTime.ToString("g"), Util.CountSize(fi.Length)));
            }

            _entries = App.Beatmaps.GetMapListFromDb(maps);
            var viewModels = _entries.Transform(false).ToList();
            for (var i = 0; i < viewModels.Count; i++)
            {
                var sb = list.First(k => k.MapIdentity.Equals(viewModels[i].GetIdentity()));
                viewModels[i].Id = i.ToString("00");
                viewModels[i].ExportFile = sb.path;
                viewModels[i].FileSize = sb.size;
                viewModels[i].ExportTime = sb.time;
            }

            ExportList.DataContext = viewModels;
        }

        private void ExportList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
        }

        private void ItemDelete_Click(object sender, RoutedEventArgs e)
        {
            var maps = GetSelectItems();
            if (maps == null) return;
            foreach (var (map, viewModel) in maps)
            {
                if (File.Exists(viewModel.ExportFile))
                {
                    File.Delete(viewModel.ExportFile);
                    var di = new FileInfo(viewModel.ExportFile).Directory;
                    if (di.Exists && di.GetFiles().Length == 0)
                        di.Delete();
                }

                DbOperator.AddMapExport(map.GetIdentity(), null);
            }

            UpdateList();
        }

        private void ItemOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelectItem(out var viewModel);
            if (map == null) return;
            Process.Start("Explorer", "/select," + viewModel.ExportFile);
        }

        private void ItemRexport_Click(object sender, RoutedEventArgs e)
        {
            var maps = GetSelectItems();
            if (maps == null) return;
            foreach (var (map, _) in maps)
                QueueEntry(map);
            while (ExportTask != null && !ExportTask.IsCanceled && !ExportTask.IsCompleted) ;

            UpdateList();
        }

        private List<(BeatmapEntry entry, BeatmapViewModel viewModel)> GetSelectItems()
        {
            if (ExportList.SelectedItem == null)
                return null;
            return (from BeatmapViewModel selectedItem in ExportList.SelectedItems
                    select (_entries.GetBeatmapsetsByFolder(selectedItem.FolderName)
                        .FirstOrDefault(k => k.Version == selectedItem.Version), selectedItem)).ToList();
        }

        private BeatmapEntry GetSelectItem(out BeatmapViewModel viewModel)
        {
            viewModel = null;
            if (ExportList.SelectedItem == null)
                return null;
            var selectedItem = (BeatmapViewModel)ExportList.SelectedItem;
            viewModel = selectedItem;
            return _entries.GetBeatmapsetsByFolder(selectedItem.FolderName)
                .FirstOrDefault(k => k.Version == selectedItem.Version);
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
            var artistOri = MetaSelect.GetOriginal(entry.Artist, entry.ArtistUnicode);
            string escapedMp3, escapedBg;
            switch (App.Config.Export.NamingStyle)
            {
                case NamingStyle.Title:
                    escapedMp3 = Escape($"{title}");
                    escapedBg = Escape($"{title}({entry.Creator})[{entry.Version}]");
                    break;
                case NamingStyle.ArtistTitle:
                    escapedMp3 = Escape($"{artist} - {title}");
                    escapedBg = Escape($"{artist} - {title}({entry.Creator})[{entry.Version}]");
                    break;
                case NamingStyle.TitleArtist:
                    escapedMp3 = Escape($"{title} - {artist}");
                    escapedBg = Escape($"{title} - {artist}({entry.Creator})[{entry.Version}]");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            string exportMp3Folder, exportBgFolder;
            switch (App.Config.Export.SortStyle)
            {
                case SortStyle.None:
                    exportMp3Folder = Domain.MusicPath;
                    exportBgFolder = Domain.BackgroundPath;
                    break;
                case SortStyle.Artist:
                    {
                        var art = Escape(artist);
                        var oriArt = Escape(artistOri);
                        var oArt = Escape(entry.Artist);
                        var tArt = Escape(entry.ArtistUnicode);
                        if (!string.IsNullOrEmpty(tArt) && Directory.Exists(Path.Combine(Domain.MusicPath, tArt)))
                            exportMp3Folder = Path.Combine(Domain.MusicPath, tArt);
                        else if (!string.IsNullOrEmpty(oArt) && Directory.Exists(Path.Combine(Domain.MusicPath, oArt)))
                            exportMp3Folder = Path.Combine(Domain.MusicPath, oArt);
                        else
                            exportMp3Folder = Path.Combine(Domain.MusicPath, string.IsNullOrEmpty(art) ? "未知艺术家" : art);
                        exportBgFolder = Path.Combine(Domain.BackgroundPath, string.IsNullOrEmpty(art) ? "未知艺术家" : art);
                        break;
                    }
                case SortStyle.Mapper:
                    {
                        var c = Escape(entry.Creator);
                        exportMp3Folder = Path.Combine(Domain.MusicPath, string.IsNullOrEmpty(c) ? "未知作者" : c);
                        exportBgFolder = Path.Combine(Domain.BackgroundPath, string.IsNullOrEmpty(c) ? "未知作者" : c);
                        break;
                    }
                case SortStyle.Source:
                    {
                        var f = Escape(entry.SongSource);
                        exportMp3Folder = Path.Combine(Domain.MusicPath, string.IsNullOrEmpty(f) ? "未知来源" : f);
                        exportBgFolder = Path.Combine(Domain.BackgroundPath, string.IsNullOrEmpty(f) ? "未知来源" : f);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            string validMp3Name = ValidateFilename(escapedMp3, Domain.MusicPath, mp3File.Extension);
            string validBgName = ValidateFilename(escapedBg, Domain.BackgroundPath, bgFile.Extension);

            if (mp3File.Exists)
                Export(mp3File, exportMp3Folder, validMp3Name);
            if (bgFile.Exists)
                Export(bgFile, exportBgFolder, validBgName);
            if (mp3File.Exists || bgFile.Exists)
                DbOperator.AddMapExport(entry.GetIdentity(), Path.Combine(exportMp3Folder, validMp3Name + mp3File.Extension));
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
            return source?.Replace(@"\", "").Replace(@"/", "").Replace(@":", "").Replace(@"*", "").Replace(@"?", "")
                .Replace("\"", "").Replace(@"<", "").Replace(@">", "").Replace(@"|", "");
        }
    }
}
