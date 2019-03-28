using Milky.OsuPlayer.Common.Data;
using OSharp.Beatmap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Common.Scanning
{
    public class OsuFileScanner
    {
        private CancellationTokenSource _scanCts;
        private bool _isScanning;
        private static readonly object ScanObject = new object();

        public async Task NewScanAndAddAsync(string path)
        {
            lock (ScanObject)
            {
                if (_isScanning)
                    return;
                _isScanning = true;
            }

            _scanCts = new CancellationTokenSource();
            await BeatmapDbOperator.RemoveLocalAllAsync();
            var dirInfo = new DirectoryInfo(path);

            foreach (var privateFolder in dirInfo.EnumerateDirectories(searchPattern: "*.*", searchOption: SearchOption.TopDirectoryOnly))
            {
                if (_scanCts.IsCancellationRequested)
                    break;
                await ScanPrivateFolderAsync(privateFolder);
            }

            lock (ScanObject)
            {
                _isScanning = false;
            }
        }

        public async Task CancelTaskAsync()
        {
            _scanCts.Cancel();
            await Task.Run(() =>
            {
                // ReSharper disable once InconsistentlySynchronizedField
                while (_isScanning)
                {
                    Thread.Sleep(1);
                }
            });
        }

        private async Task ScanPrivateFolderAsync(DirectoryInfo privateFolder)
        {
            using (var dbOperator = new BeatmapDbOperator())
            {
                foreach (var fileInfo in privateFolder.EnumerateFiles(searchPattern: "*.osu", searchOption: SearchOption.TopDirectoryOnly))
                {
                    if (_scanCts.IsCancellationRequested)
                        return;

                    var osuFile = await OsuFile.ReadFromFileAsync(fileInfo.FullName);
                    await AddFileAsync(dbOperator, osuFile, fileInfo);
                }
            }
        }

        private async Task AddFileAsync(BeatmapDbOperator dbOperator, OsuFile osuFile, FileInfo fileInfo)
        {
            var beatmap = Data.EF.Model.Beatmap.ParseFromOSharp(osuFile);
            beatmap.BeatmapFileName = fileInfo.Name;
            beatmap.LastModifiedTime = fileInfo.LastWriteTime;
            beatmap.FolderName = fileInfo.Directory.Name;
            await dbOperator.AddNewMapAsync(beatmap);
        }
    }
}
