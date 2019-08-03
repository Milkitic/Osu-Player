using Milky.OsuPlayer.Common.Data;
using OSharp.Beatmap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Data.EF;

namespace Milky.OsuPlayer.Common.Scanning
{
    public class OsuFileScanner
    {
        public FileScannerViewModel ViewModel { get; set; } = new FileScannerViewModel();
        private CancellationTokenSource _scanCts;
        private BeatmapDbOperator _beatmapDbOperator = new BeatmapDbOperator();

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
            await _beatmapDbOperator.RemoveLocalAllAsync();
            var dirInfo = new DirectoryInfo(path);

            foreach (var privateFolder in dirInfo.EnumerateDirectories(searchPattern: "*.*", searchOption: SearchOption.TopDirectoryOnly))
            {
                if (_scanCts.IsCancellationRequested)
                    break;
                await ScanPrivateFolderAsync(privateFolder);
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

            _scanCts.Cancel();
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

            foreach (var fileInfo in privateFolder.EnumerateFiles(searchPattern: "*.osu", searchOption: SearchOption.TopDirectoryOnly))
            {
                if (_scanCts.IsCancellationRequested)
                    return;

                var osuFile = await OsuFile.ReadFromFileAsync(fileInfo.FullName);
                await AddFileAsync(osuFile, fileInfo);
            }
        }

        private async Task AddFileAsync(OsuFile osuFile, FileInfo fileInfo)
        {
            var beatmap = Data.EF.Model.Beatmap.ParseFromOSharp(osuFile);
            beatmap.BeatmapFileName = fileInfo.Name;
            beatmap.LastModifiedTime = fileInfo.LastWriteTime;
            beatmap.FolderName = fileInfo.Directory.Name;
            beatmap.InOwnFolder = true;
            await _beatmapDbOperator.AddNewMapAsync(beatmap);
        }
    }
}
