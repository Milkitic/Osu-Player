using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Services;

namespace Milky.OsuPlayer.Common.Scanning
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
            _playerData.TryRemoveLocalAll();

            var dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists)
            {
                foreach (var privateFolder in dirInfo.EnumerateDirectories(searchPattern: "*.*",
                             searchOption: SearchOption.TopDirectoryOnly))
                {
                    if (_scanCts.IsCancellationRequested)
                        break;
                    await ScanPrivateFolderAsync(privateFolder);
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

        private async Task ScanPrivateFolderAsync(DirectoryInfo privateFolder)
        {
            var beatmaps = new List<Beatmap>();
            foreach (var fileInfo in privateFolder.EnumerateFiles("*.osu", SearchOption.TopDirectoryOnly))
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

                    var beatmap = GetBeatmapObj(osuFile, fileInfo);
                    beatmaps.Add(beatmap);
                }
                catch (Exception ex)
                {
                    s_logger.Error(ex, "Error during scanning file, ignored {0}", fileInfo.FullName);
                }
            }

            try
            {
                _playerData.TryAddNewMaps(beatmaps);
            }
            catch (Exception ex)
            {
                s_logger.Error(ex);
                throw;
            }
        }

        private Beatmap GetBeatmapObj(LocalOsuFile osuFile, FileInfo fileInfo)
        {
            var beatmap = BeatmapExtension.ParseFromOSharp(osuFile);
            beatmap.BeatmapFileName = fileInfo.Name;
            beatmap.LastModifiedTime = fileInfo.LastWriteTime;
            beatmap.FolderName = fileInfo.Directory?.Name;
            beatmap.InOwnDb = true;
            return beatmap;
        }
    }
}