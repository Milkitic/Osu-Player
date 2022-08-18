using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Anotar.NLog;
using Coosu.Beatmap;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.UiComponents.NotificationComponent;
using Milki.OsuPlayer.ViewModels;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.Pages;

/// <summary>
/// ExportPage.xaml 的交互逻辑
/// </summary>
public partial class ExportPage : Page
{
    //Page view model
    private static bool _hasTaskSuccess;
    private readonly MainWindow _mainWindow;

    public ExportPageViewModel ViewModel { get; }
    public static readonly ConcurrentQueue<PlayItem> TaskQueue = new();
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

    public static string MusicDir => AppSettings.Default.ExportSection.MusicDir;
    public static string BackgroundDir => AppSettings.Default.ExportSection.BackgroundDir;

    public static bool IsTaskBusy =>
        ExportTask != null && !ExportTask.IsCanceled && !ExportTask.IsCompleted && !ExportTask.IsFaulted;

    public ExportPage()
    {
        InitializeComponent();
        _mainWindow = (MainWindow)Application.Current.MainWindow;
        ViewModel = (ExportPageViewModel)DataContext;
        ViewModel.ExportPath = AppSettings.Default.ExportSection.MusicDir;
    }

    public static void QueueBeatmaps(IEnumerable<PlayItem> beatmaps)
    {
        foreach (var beatmap in beatmaps)
            TaskQueue.Enqueue(beatmap);

        StartTask();
    }

    public static void QueueBeatmap(PlayItem beatmap)
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

    private static async Task CopyFileAsync(PlayItem playItem)
    {
        var playItemDetail = playItem.PlayItemDetail;
        var folder = PathUtils.GetFullPath(playItem.StandardizedFolder,
            AppSettings.Default.GeneralSection.OsuSongDir);
        try
        {
            var mp3FileInfo = new FileInfo(Path.Combine(folder, playItemDetail.AudioFileName));
            LocalOsuFile osuFile;
            try
            {
                osuFile = await OsuFile.ReadFromFileAsync(Path.Combine(folder, playItemDetail.BeatmapFileName), options =>
                {
                    options.IncludeSection("Events");
                    options.IgnoreSample();
                    options.IgnoreStoryboard();
                });
            }
            catch (Exception ex)
            {
                return;
            }

            var bgFileInfo = new FileInfo(Path.Combine(folder, osuFile.Events.BackgroundInfo.Filename));

            var artistUtf = MetaString.GetUnicode(playItemDetail.Artist, playItemDetail.ArtistUnicode);
            var titleUtf = MetaString.GetUnicode(playItemDetail.Title, playItemDetail.TitleUnicode);
            var artistAsc = MetaString.GetOriginal(playItemDetail.Artist, playItemDetail.ArtistUnicode);
            var creator = playItemDetail.Creator;
            var version = playItemDetail.Version;
            var source = playItemDetail.Source;

            ConstructNameWithEscaping(out var escapedMp3, out var escapedBg,
                titleUtf, artistUtf, creator, version);

            GetExportFolder(out var exportMp3Folder, out var exportBgFolder,
                new MetaString(artistAsc, artistUtf), creator, source);

            string exportMp3Name = ValidateFilename(escapedMp3, MusicDir, mp3FileInfo.Extension);
            string exportBgName = ValidateFilename(escapedBg, BackgroundDir, bgFileInfo.Extension);

            if (mp3FileInfo.Exists)
                Export(mp3FileInfo, exportMp3Folder, exportMp3Name);
            if (bgFileInfo.Exists)
                Export(bgFileInfo, exportBgFolder, exportBgName);
            if (mp3FileInfo.Exists || bgFileInfo.Exists)
            {
                await using var appDbContext = new ApplicationDbContext();
                await appDbContext.AddOrUpdateExport(new ExportItem
                {
                    Beatmap = playItemDetail,
                    BeatmapId = playItemDetail.Id,
                    ExportPath = Path.Combine(exportMp3Folder, exportMp3Name + mp3FileInfo.Extension),
                    IsValid = true,
                });
            }
        }
        catch (Exception ex)
        {
            LogTo.ErrorException($"Error while exporting beatmap: {playItem.StandardizedPath}", ex);
            Notification.Push($"Error while exporting beatmap: {playItem.StandardizedPath}\r\n{ex.Message}");
        }
    }

    private static void GetExportFolder(out string exportMp3Folder, out string exportBgFolder,
        MetaString artist, string creator, string source)
    {
        switch (AppSettings.Default.ExportSection.ExportGroupStyle)
        {
            case ExportGroupStyle.None:
                exportMp3Folder = MusicDir;
                exportBgFolder = BackgroundDir;
                break;
            case ExportGroupStyle.Artist:
                {
                    var escArtistAsc = Escape(artist.Origin);
                    var escArtistUtf = Escape(artist.Unicode);
                    if (string.IsNullOrEmpty(escArtistAsc))
                        escArtistAsc = "未知艺术家";
                    if (string.IsNullOrEmpty(escArtistUtf))
                        escArtistUtf = "未知艺术家";

                    if (Directory.Exists(Path.Combine(MusicDir, escArtistUtf)))
                        exportMp3Folder = Path.Combine(MusicDir, escArtistUtf);
                    else if (Directory.Exists(Path.Combine(MusicDir, escArtistAsc)))
                        exportMp3Folder = Path.Combine(MusicDir, escArtistAsc);
                    else
                        exportMp3Folder = Path.Combine(MusicDir, escArtistUtf);

                    exportBgFolder = Path.Combine(BackgroundDir, escArtistUtf);
                    break;
                }
            case ExportGroupStyle.Mapper:
                {
                    var escCreator = Escape(creator);
                    if (string.IsNullOrEmpty(escCreator))
                        escCreator = "未知作者";

                    exportMp3Folder = Path.Combine(MusicDir, escCreator);
                    exportBgFolder = Path.Combine(BackgroundDir, escCreator);
                    break;
                }
            case ExportGroupStyle.Source:
                {
                    var escSource = Escape(source);
                    if (string.IsNullOrEmpty(escSource))
                        escSource = "未知来源";

                    exportMp3Folder = Path.Combine(MusicDir, escSource);
                    exportBgFolder = Path.Combine(BackgroundDir, escSource);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(AppSettings.Default.ExportSection.ExportGroupStyle),
                    AppSettings.Default.ExportSection.ExportGroupStyle, null);
        }
    }

    private static void ConstructNameWithEscaping(out string originAudio, out string originBack,
        string title, string artist, string creator, string version)
    {
        switch (AppSettings.Default.ExportSection.ExportNamingStyle)
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
                throw new ArgumentOutOfRangeException(nameof(AppSettings.Default.ExportSection.ExportNamingStyle),
                    AppSettings.Default.ExportSection.ExportNamingStyle, null);
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