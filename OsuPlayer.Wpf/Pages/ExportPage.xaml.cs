using OSharp.Beatmap;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Models;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using Path = System.IO.Path;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// ExportPage.xaml 的交互逻辑
    /// </summary>
    public partial class ExportPage : Page
    {
        //Page view model
        private static bool _hasTaskSuccess;
        private readonly MainWindow _mainWindow;

        public ExportPageViewModel ViewModel { get; }
        public static readonly ConcurrentQueue<BeatmapEntry> TaskQueue = new ConcurrentQueue<BeatmapEntry>();
        public static Task ExportTask;
        public static bool Overlap = true;

        public static bool HasTaskSuccess
        {
            get
            {
                bool flag = _hasTaskSuccess;
                _hasTaskSuccess = false;
                return flag;
            }
            private set => _hasTaskSuccess = value;
        }

        public static bool IsTaskBusy =>
            ExportTask != null && !ExportTask.IsCanceled && !ExportTask.IsCompleted && !ExportTask.IsFaulted;

        public ExportPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            ViewModel = (ExportPageViewModel)DataContext;
            ViewModel.ExportPath = App.Config.Export.MusicPath;
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
                    HasTaskSuccess = true;
                }
            });
        }

        private static void CopyFile(BeatmapEntry entry)
        {
            string folder = Path.Combine(Domain.OsuSongPath, entry.FolderName);
            FileInfo mp3File = new FileInfo(Path.Combine(folder, entry.AudioFileName));
            var osuFile = OsuFile.ReadFromFile(Path.Combine(folder, entry.BeatmapFileName));
            FileInfo bgFile = new FileInfo(Path.Combine(folder, osuFile.Events.BackgroundInfo.Filename));

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
                DbOperate.AddMapExport(entry.GetIdentity(), Path.Combine(exportMp3Folder, validMp3Name + mp3File.Extension));
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

        void OpenCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            string command, targetobj;
            command = ((RoutedCommand)e.Command).Name;
            targetobj = ((FrameworkElement)target).Name;
            MessageBox.Show("The " + command + " command has been invoked on target object " + targetobj);
        }
        void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
    }
}
