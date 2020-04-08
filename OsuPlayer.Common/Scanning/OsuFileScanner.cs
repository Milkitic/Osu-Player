using Milky.OsuPlayer.Data;
using OSharp.Beatmap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.Common.Scanning
{
    public class OsuFileScanner
    {
        public FileScannerViewModel ViewModel { get; set; } = new FileScannerViewModel();
        private CancellationTokenSource _scanCts;
        private AppDbOperator _beatmapDbOperator = new AppDbOperator();

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
            _beatmapDbOperator.RemoveLocalAll();
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
                            options.IncludeSection("General", "Metadata", "TimingPoints", "Difficulty", "HitObjects",
                                "Events");
                            options.IgnoreSample();
                            options.IgnoreStoryboard();
                        });
                    if (!osuFile.ReadSuccess)
                    {
                        Console.WriteLine($"Skipped '{fileInfo.FullName}': {osuFile.ReadException.Message}");
                        continue;
                    }

                    var beatmap = GetBeatmapObj(osuFile, fileInfo);
                    beatmaps.Add(beatmap);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message} ignored '{fileInfo.FullName}'");
                }
            }

            _beatmapDbOperator.AddNewMaps(beatmaps);
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
