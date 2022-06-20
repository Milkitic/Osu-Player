using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.Common.Scanning
{
    public class OsuFileScanner
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public FileScannerViewModel ViewModel { get; set; } = new FileScannerViewModel();
        private CancellationTokenSource _scanCts;
        private readonly AppDbOperator _dbOperator = new AppDbOperator();

        private static readonly object ScanObject = new object();
        private static readonly object CancelObject = new object();

        public async Task NewScanAndAddAsync(string path)
        {
            lock (ScanObject)
            {
                if (ViewModel.IsScanning)
                    return;
                ViewModel.IsScanning = true;
            }

            _scanCts = new CancellationTokenSource();
            _dbOperator.RemoveLocalAll();
            var dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists)
            {
                foreach (var privateFolder in dirInfo.EnumerateDirectories(searchPattern: "*.*", searchOption: SearchOption.TopDirectoryOnly))
                {
                    if (_scanCts.IsCancellationRequested)
                        break;
                    await ScanPrivateFolderAsync(privateFolder);
                }
            }

            lock (ScanObject)
            {
                ViewModel.IsScanning = false;
            }
        }

        public async Task CancelTaskAsync()
        {
            lock (CancelObject)
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

            lock (CancelObject)
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
                    Logger.Error(ex, "Error during scanning file, ignored {0}", fileInfo.FullName);
                }
            }

            try
            {
                _dbOperator.AddNewMaps(beatmaps);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
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
