using System.IO;
using Anotar.NLog;
using Coosu.Beatmap;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.Services;

public class OsuFileScanningService
{
    public class FileScannerViewModel : VmBase
    {
        private bool _isScanning;
        private bool _isCanceling;

        public bool IsScanning
        {
            get => _isScanning;
            internal set => this.RaiseAndSetIfChanged(ref _isScanning, value);
        }

        public bool IsCanceling
        {
            get => _isCanceling;
            set => this.RaiseAndSetIfChanged(ref _isCanceling, value);
        }
    }

    public FileScannerViewModel ViewModel { get; set; } = new FileScannerViewModel();
    private CancellationTokenSource _scanCts;

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
        await using var dbContext = new ApplicationDbContext();
        await dbContext.RemoveFolderAll();
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
        var beatmaps = new List<PlayItemDetail>();
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
                //if (!osuFile.ReadSuccess)
                //{
                //    Logger.Warn(osuFile.ReadException, "Osu file format error, skipped {0}", fileInfo.FullName);
                //    continue;
                //}

                var beatmap = GetPlayItemByOsuFile(osuFile, fileInfo);
                beatmaps.Add(beatmap);
            }
            catch (Exception ex)
            {
                LogTo.ErrorException($"Error during scanning file, ignored {fileInfo.FullName}", ex);
            }
        }

        try
        {
            await using var dbContext = new ApplicationDbContext();
            await dbContext.AddNewBeatmaps(beatmaps);
        }
        catch (Exception ex)
        {
            LogTo.ErrorException("", ex);
            throw;
        }
    }

    private static PlayItemDetail GetPlayItemByOsuFile(LocalOsuFile osuFile, FileInfo fileInfo)
    {
        var playItem = BeatmapConvertExtension.ParseFromOSharp(osuFile);
        playItem.BeatmapFileName = fileInfo.Name;
        playItem.LastModifiedTime = fileInfo.LastWriteTime;
        playItem.FolderNameOrPath = fileInfo.Directory?.Name;
        playItem.InOwnDb = true;
        return playItem;
    }
}