using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Core.Configuration;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared.Models;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Path = System.IO.Path;

namespace Milky.OsuPlayer.Services
{
    public class ExportService : IExportService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IPlayerDataService _playerData;
        private readonly ConcurrentQueue<Beatmap> _taskQueue = new();
        private Task _exportTask;
        private readonly object _lock = new();

        public bool IsTaskBusy
        {
            get
            {
                lock (_lock)
                {
                    return _exportTask != null && !_exportTask.IsCanceled && !_exportTask.IsCompleted && !_exportTask.IsFaulted;
                }
            }
        }

        public bool Overlap { get; set; } = true;

        public event EventHandler TaskSuccess;

        public ExportService(IPlayerDataService playerData)
        {
            _playerData = playerData;
        }

        public void QueueEntry(Beatmap entry)
        {
            if (entry == null) return;
            _taskQueue.Enqueue(entry);
            StartTask();
        }

        public void QueueEntries(IEnumerable<Beatmap> entries)
        {
            if (entries == null) return;
            foreach (var entry in entries)
            {
                _taskQueue.Enqueue(entry);
            }
            StartTask();
        }

        private void StartTask()
        {
            lock (_lock)
            {
                if (_exportTask != null && !_exportTask.IsCanceled && !_exportTask.IsCompleted)
                    return;

                _exportTask = Task.Run(async () =>
                {
                    while (!_taskQueue.IsEmpty)
                    {
                        if (!_taskQueue.TryDequeue(out var entry))
                            continue;
                        await CopyFileAsync(entry);
                        TaskSuccess?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
        }

        private async Task CopyFileAsync(Beatmap entry)
        {
            try
            {
                var folder = entry.GetFolder(out _, out _);
                var mp3FileInfo = new FileInfo(Path.Combine(folder, entry.AudioFileName));
                var osuFile = await OsuFile.ReadFromFileAsync(Path.Combine(folder, entry.BeatmapFileName), options =>
                {
                    options.IncludeSection("Events");
                    options.IgnoreSample();
                    options.IgnoreStoryboard();
                });

                var bgFileInfo = new FileInfo(Path.Combine(folder, osuFile.Events.BackgroundInfo.Filename));

                var artistUtf = MetaString.GetUnicode(entry.Artist, entry.ArtistUnicode);
                var titleUtf = MetaString.GetUnicode(entry.Title, entry.TitleUnicode);
                var artistAsc = MetaString.GetOriginal(entry.Artist, entry.ArtistUnicode);
                var creator = entry.Creator;
                var version = entry.Version;
                var source = entry.SongSource;

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
                    await _playerData.TryAddMapExportAsync(entry.GetIdentity(),
                        Path.Combine(exportMp3Folder, exportMp3Name + mp3FileInfo.Extension));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while exporting beatmap: {0}", entry.GetIdentity());
                Notification.Push($"Error while exporting beatmap: {entry.GetIdentity()}\r\n{ex.Message}");
            }
        }

        private void GetExportFolder(out string exportMp3Folder, out string exportBgFolder,
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

        private void ConstructNameWithEscaping(out string originAudio, out string originBack,
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

        private void Export(FileInfo originFile, string outputDir, string outputFile)
        {
            var path = Path.Combine(outputDir, outputFile + originFile.Extension);
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            if (Overlap && File.Exists(path))
                File.Delete(path);
            File.Copy(originFile.FullName, path);
            UpdateFileTime(path);
        }

        private string ValidateFilename(string escaped, string dirPath, string ext)
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

        private void UpdateFileTime(string mp3Path)
        {
            var time = DateTime.Now;
            _ = new FileInfo(mp3Path)
            {
                CreationTime = time,
                LastAccessTime = time,
                LastWriteTime = time
            };
        }

        private string Escape(string source)
        {
            return source?.Replace(@"\", "").Replace(@"/", "").Replace(@":", "").Replace(@"*", "").Replace(@"?", "")
                .Replace("\"", "").Replace(@"<", "").Replace(@">", "").Replace(@"|", "");
        }
    }
}
