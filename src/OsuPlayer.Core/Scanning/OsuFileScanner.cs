using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Services;

namespace Milky.OsuPlayer.Core.Scanning
{
    public class OsuFileScanner
    {
        private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly Lock s_scanObject = new Lock();
        private static readonly Lock s_cancelObject = new Lock();

        public FileScannerViewModel ViewModel { get; set; } = new FileScannerViewModel();
        private CancellationTokenSource _scanCts;
        private readonly IPlayerDataStore _playerData;

        public OsuFileScanner()
            : this(new PlayerDataService())
        {
        }

        public OsuFileScanner(IPlayerDataStore playerData)
        {
            _playerData = playerData;
        }

        public async Task NewScanAndAddAsync(string path)
        {
            lock (s_scanObject)
            {
                if (ViewModel.IsScanning)
                    return;
                ViewModel.IsScanning = true;
            }

            _scanCts = new CancellationTokenSource();
            await _playerData.TryRemoveLocalAllAsync();

            var dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists)
            {
                foreach (var group in EnumerateOsuFiles(dirInfo).GroupBy(k => k.DirectoryName))
                {
                    if (_scanCts.IsCancellationRequested)
                        break;
                    await ScanFolderAsync(dirInfo, group);
                }
            }

            lock (s_scanObject)
            {
                ViewModel.IsScanning = false;
            }
        }

        public async Task CancelTaskAsync()
        {
            lock (s_cancelObject)
            {
                if (ViewModel.IsCanceling)
                    return;
                ViewModel.IsCanceling = true;
            }

            _scanCts?.Cancel();
            await Task.Run(() =>
            {
                // ReSharper disable once InconsistentlySynchronizedField
                while (ViewModel.IsScanning)
                {
                    Thread.Sleep(1);
                }
            });

            lock (s_cancelObject)
            {
                ViewModel.IsCanceling = false;
            }
        }

        private async Task ScanFolderAsync(DirectoryInfo rootFolder, IEnumerable<FileInfo> osuFiles)
        {
            var beatmaps = new List<Beatmap>();
            foreach (var fileInfo in osuFiles)
            {
                if (_scanCts.IsCancellationRequested)
                    return;
                try
                {
                    var osuFile = await OsuFile.ReadFromFileAsync(fileInfo.FullName,
                        options =>
                        {
                            options.IncludeSection("General");
                            options.IncludeSection("Metadata");
                            options.IncludeSection("TimingPoints");
                            options.IncludeSection("Difficulty");
                            options.IncludeSection("HitObjects");
                            options.IncludeSection("Events");
                            options.IgnoreSample();
                            options.IgnoreStoryboard();
                        });

                    var beatmap = GetBeatmapObj(rootFolder, osuFile, fileInfo);
                    beatmaps.Add(beatmap);
                }
                catch (Exception ex)
                {
                    s_logger.Error(ex, "Error during scanning file, ignored {0}", fileInfo.FullName);
                }
            }

            try
            {
                await _playerData.TryAddNewMapsAsync(beatmaps);
            }
            catch (Exception ex)
            {
                s_logger.Error(ex);
                throw;
            }
        }

        private static IEnumerable<FileInfo> EnumerateOsuFiles(DirectoryInfo rootFolder)
        {
            try
            {
                return rootFolder.EnumerateFiles("*.osu", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "Error during enumerating osu files, ignored {0}", rootFolder.FullName);
                return Enumerable.Empty<FileInfo>();
            }
        }

        private Beatmap GetBeatmapObj(DirectoryInfo rootFolder, LocalOsuFile osuFile, FileInfo fileInfo)
        {
            var beatmap = BeatmapExtension.ParseFromOSharp(osuFile);
            beatmap.BeatmapFileName = fileInfo.Name;
            beatmap.LastModifiedTime = fileInfo.LastWriteTime;
            beatmap.FolderName = GetRelativeFolderName(rootFolder, fileInfo);
            beatmap.InOwnDb = true;
            return beatmap;
        }

        private static string GetRelativeFolderName(DirectoryInfo rootFolder, FileInfo fileInfo)
        {
            var directory = fileInfo.Directory?.FullName;
            if (string.IsNullOrWhiteSpace(directory))
            {
                return string.Empty;
            }

            var relativePath = Path.GetRelativePath(rootFolder.FullName, directory);
            if (relativePath == ".")
            {
                return string.Empty;
            }

            return relativePath;
        }
    }
}
