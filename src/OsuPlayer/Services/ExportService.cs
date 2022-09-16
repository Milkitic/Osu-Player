using System.Collections.Concurrent;
using System.IO;
using Anotar.NLog;
using Coosu.Beatmap;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.UiComponents.NotificationComponent;

namespace Milki.OsuPlayer.Services;

internal class ExportService
{
    private readonly ConcurrentQueue<PlayItem> _taskQueue = new();
    private Task _exportTask;
    private bool _hasTaskSuccess;

    public bool HasTaskSuccess
    {
        get
        {
            var flag = _hasTaskSuccess;
            _hasTaskSuccess = false;
            return flag;
        }
        private set => _hasTaskSuccess = value;
    }

    public bool IsTaskBusy => _exportTask is { IsCanceled: false, IsCompleted: false, IsFaulted: false };

    public string MusicDir => AppSettings.Default.ExportSection.DirMusic;
    public bool Overwrite { get; } = true;

    private string BackgroundDir => AppSettings.Default.ExportSection.DirBackground;

    public void QueueBeatmaps(IEnumerable<PlayItem> beatmaps)
    {
        foreach (var beatmap in beatmaps)
            _taskQueue.Enqueue(beatmap);

        StartTask();
    }

    public void QueueBeatmap(PlayItem beatmap)
    {
        _taskQueue.Enqueue(beatmap);
        StartTask();
    }

    private void StartTask()
    {
        if (_exportTask != null && !_exportTask.IsCanceled && !_exportTask.IsCompleted)
            return;
        _exportTask = Task.Run(async () =>
        {
            while (!_taskQueue.IsEmpty)
            {
                if (!_taskQueue.TryDequeue(out var beatmap))
                    continue;
                await CopyFileAsync(beatmap);
                HasTaskSuccess = true;
            }
        });
    }

    private async ValueTask CopyFileAsync(PlayItem playItem)
    {
        var playItemDetail = playItem.PlayItemDetail;
        var folder = PathUtils.GetFullPath(playItem.StandardizedFolder,
            AppSettings.Default.GeneralSection.DirOsuSong);
        try
        {
            var mp3FileInfo = new FileInfo(Path.Combine(folder, playItemDetail.AudioFileName));
            var osuFile = await OsuFile.ReadFromFileAsync(Path.Combine(folder, playItemDetail.BeatmapFileName),
                options =>
                {
                    options.IncludeSection("Events");
                    options.IgnoreSample();
                    options.IgnoreStoryboard();
                });

            var filename = osuFile.Events?.BackgroundInfo?.Filename;
            var bgFileInfo = new FileInfo(filename == null ? "." : Path.Combine(folder, filename));

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

            var exportMp3Name = ValidateFilename(escapedMp3, MusicDir, mp3FileInfo.Extension);
            var exportBgName = ValidateFilename(escapedBg, BackgroundDir, bgFileInfo.Extension);

            if (mp3FileInfo.Exists) Export(mp3FileInfo, exportMp3Folder, exportMp3Name);

            if (bgFileInfo.Exists) Export(bgFileInfo, exportBgFolder, exportBgName);

            if (mp3FileInfo.Exists || bgFileInfo.Exists)
            {
                var appDbContext = ServiceProviders.GetApplicationDbContext();
                await appDbContext.AddOrUpdateExportAsync(new ExportItem
                {
                    Size = mp3FileInfo.Exists ? mp3FileInfo.Length : bgFileInfo.Length,
                    ExportPath = Path.Combine(exportMp3Folder, exportMp3Name + mp3FileInfo.Extension),
                    ExportTime = mp3FileInfo.Exists ? mp3FileInfo.LastAccessTime : bgFileInfo.LastAccessTime,
                    Title = playItem.PlayItemDetail.AutoTitle,
                    Artist = playItem.PlayItemDetail.AutoArtist,
                    Creator = playItem.PlayItemDetail.Creator,
                    Version = playItem.PlayItemDetail.Version,
                    PlayItemStandardizedPath = playItem.StandardizedPath,
                    PlayItemId = playItem.Id
                });
            }
        }
        catch (Exception ex)
        {
            LogTo.ErrorException($"Error while exporting beatmap: {playItem.StandardizedPath}", ex);
            Notification.Push($"Error while exporting beatmap: {playItem.StandardizedPath}\r\n{ex.Message}");
        }
    }

    private void GetExportFolder(out string exportMp3Folder, out string exportBgFolder,
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

    private void ConstructNameWithEscaping(out string originAudio, out string originBack,
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

    private void Export(FileInfo originFile, string outputDir, string outputFile)
    {
        var path = Path.Combine(outputDir, outputFile + originFile.Extension);
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        if (Overwrite && File.Exists(path)) File.Delete(path);
        File.Copy(originFile.FullName, path);
        UpdateFileTime(path);
    }

    private string ValidateFilename(string escaped, string dirPath, string ext)
    {
        if (Overwrite)
            return escaped;
        var validName = escaped;
        var i = 1;
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

    private static string Escape(string path)
    {
        return Coosu.Shared.IO.PathUtils.EscapeFileName(path);
    }
}