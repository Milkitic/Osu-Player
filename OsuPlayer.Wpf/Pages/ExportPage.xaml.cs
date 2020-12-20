using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared.Models;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using OSharp.Beatmap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Milky.OsuPlayer.Data;
using Path = System.IO.Path;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// ExportPage.xaml 的交互逻辑
    /// </summary>
    public partial class ExportPage : Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        //Page view model
        private static bool _hasTaskSuccess;
        private readonly MainWindow _mainWindow;

        public ExportPageViewModel ViewModel { get; }
        public static readonly ConcurrentQueue<Beatmap> TaskQueue = new ConcurrentQueue<Beatmap>();
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

        public ExportPage()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            ViewModel = (ExportPageViewModel)DataContext;
            ViewModel.ExportPath = AppSettings.Default.Export.MusicPath;
        }

        public static void QueueBeatmaps(IEnumerable<Beatmap> beatmaps)
        {
            foreach (var beatmap in beatmaps)
                TaskQueue.Enqueue(beatmap);

            StartTask();
        }

        public static void QueueBeatmap(Beatmap beatmap)
        {
            TaskQueue.Enqueue(beatmap);
            StartTask();
        }

        private static void StartTask()
        {
            if (ExportTask != null && !ExportTask.IsCanceled && !ExportTask.IsCompleted)
                return;
            ExportTask = Task.Run(async () =>
            {
                while (!TaskQueue.IsEmpty)
                {
                    if (!TaskQueue.TryDequeue(out var beatmap))
                        continue;
                    await CopyFileAsync(beatmap);
                    HasTaskSuccess = true;
                }
            });
        }

        private static async Task CopyFileAsync(Beatmap beatmap)
        {
            try
            {
                var folder = beatmap.GetFolder(out _, out _);
                var mp3FileInfo = new FileInfo(Path.Combine(folder, beatmap.AudioFileName));
                var osuFile = await OsuFile.ReadFromFileAsync(Path.Combine(folder, beatmap.BeatmapFileName), options =>
                {
                    options.IncludeSection("Events");
                    options.IgnoreSample();
                    options.IgnoreStoryboard();
                });
                if (!osuFile.ReadSuccess) return;

                var bgFileInfo = new FileInfo(Path.Combine(folder, osuFile.Events.BackgroundInfo.Filename));

                var artistUtf = MetaString.GetUnicode(beatmap.Artist, beatmap.ArtistUnicode);
                var titleUtf = MetaString.GetUnicode(beatmap.Title, beatmap.TitleUnicode);
                var artistAsc = MetaString.GetOriginal(beatmap.Artist, beatmap.ArtistUnicode);
                var creator = beatmap.Creator;
                var version = beatmap.Version;
                var source = beatmap.SongSource;

                ConstructNameWithEscaping(out var escapedMp3, out var escapedBg,
                    titleUtf, artistUtf, creator, version);

                GetExportFolder(out var exportMp3Folder, out var exportBgFolder,
                    new MetaString(artistAsc, artistUtf), creator, source);

                string exportMp3Name = ValidateFilename(escapedMp3, Domain.MusicPath, mp3FileInfo.Extension);
                string exportBgName = ValidateFilename(escapedBg, Domain.BackgroundPath, bgFileInfo.Extension);

                if (mp3FileInfo.Exists)
                    Export(mp3FileInfo, exportMp3Folder, exportMp3Name);
                if (bgFileInfo.Exists)
                    Export(bgFileInfo, exportBgFolder, exportBgName);
                if (mp3FileInfo.Exists || bgFileInfo.Exists)
                {
                    await using var appDbContext = new ApplicationDbContext();
                    await appDbContext.AddOrUpdateExport(new BeatmapExport
                    {
                        Beatmap = beatmap,
                        BeatmapId = beatmap.Id,
                        ExportPath = Path.Combine(exportMp3Folder, exportMp3Name + mp3FileInfo.Extension),
                        IsValid = true,
                        Id = Guid.NewGuid()
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while exporting beatmap: {0}", beatmap.ToString());
                Notification.Push($"Error while exporting beatmap: {beatmap.ToString()}\r\n{ex.Message}");
            }
        }

        private static void GetExportFolder(out string exportMp3Folder, out string exportBgFolder,
            MetaString artist, string creator, string source)
        {
            switch (AppSettings.Default.Export.ExportGroupStyle)
            {
                case ExportGroupStyle.None:
                    exportMp3Folder = Domain.MusicPath;
                    exportBgFolder = Domain.BackgroundPath;
                    break;
                case ExportGroupStyle.Artist:
                    {
                        var escArtistAsc = Escape(artist.Origin);
                        var escArtistUtf = Escape(artist.Unicode);
                        if (string.IsNullOrEmpty(escArtistAsc))
                            escArtistAsc = "未知艺术家";
                        if (string.IsNullOrEmpty(escArtistUtf))
                            escArtistUtf = "未知艺术家";

                        if (Directory.Exists(Path.Combine(Domain.MusicPath, escArtistUtf)))
                            exportMp3Folder = Path.Combine(Domain.MusicPath, escArtistUtf);
                        else if (Directory.Exists(Path.Combine(Domain.MusicPath, escArtistAsc)))
                            exportMp3Folder = Path.Combine(Domain.MusicPath, escArtistAsc);
                        else
                            exportMp3Folder = Path.Combine(Domain.MusicPath, escArtistUtf);

                        exportBgFolder = Path.Combine(Domain.BackgroundPath, escArtistUtf);
                        break;
                    }
                case ExportGroupStyle.Mapper:
                    {
                        var escCreator = Escape(creator);
                        if (string.IsNullOrEmpty(escCreator))
                            escCreator = "未知作者";

                        exportMp3Folder = Path.Combine(Domain.MusicPath, escCreator);
                        exportBgFolder = Path.Combine(Domain.BackgroundPath, escCreator);
                        break;
                    }
                case ExportGroupStyle.Source:
                    {
                        var escSource = Escape(source);
                        if (string.IsNullOrEmpty(escSource))
                            escSource = "未知来源";

                        exportMp3Folder = Path.Combine(Domain.MusicPath, escSource);
                        exportBgFolder = Path.Combine(Domain.BackgroundPath, escSource);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(AppSettings.Default.Export.ExportGroupStyle),
                        AppSettings.Default.Export.ExportGroupStyle, null);
            }
        }

        private static void ConstructNameWithEscaping(out string originAudio, out string originBack,
            string title, string artist, string creator, string version)
        {
            switch (AppSettings.Default.Export.ExportNamingStyle)
            {
                case ExportNamingStyle.Title:
                    originAudio = Escape($"{title}");
                    originBack = Escape($"{title}({creator})[{version}]");
                    break;
                case ExportNamingStyle.ArtistTitle:
                    originAudio = Escape($"{artist} - {title}");
                    originBack = Escape($"{artist} - {title}({creator})[{version}]");
                    break;
                case ExportNamingStyle.TitleArtist:
                    originAudio = Escape($"{title} - {artist}");
                    originBack = Escape($"{title} - {artist}({creator})[{version}]");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(AppSettings.Default.Export.ExportNamingStyle),
                        AppSettings.Default.Export.ExportNamingStyle, null);
            }
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
            if (Overlap)
                return escaped;
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

        private void OpenCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            string command, targetobj;
            command = ((RoutedCommand)e.Command).Name;
            targetobj = ((FrameworkElement)target).Name;
            MessageBox.Show("The " + command + " command has been invoked on target object " + targetobj);
        }

        private void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateList.Execute(null);
        }
    }
}
