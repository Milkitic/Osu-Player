using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Anotar.NLog;
using Coosu.Beatmap;
using Coosu.Beatmap.Sections.GamePlay;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Shared.Utils;

namespace Milki.OsuPlayer.Services;

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

public class OsuFileScanningService
{
    public FileScannerViewModel ViewModel { get; set; } = new FileScannerViewModel();
    private CancellationTokenSource _scanCts;

    private static readonly object ScanObject = new object();
    private static readonly object CancelObject = new object();

    public async Task ScanAndSyncAsync(string path)
    {
        var dbFolder = Path.GetFullPath(AppSettings.Default.GeneralSection.OsuSongDir);
        var customFolder = Path.GetFullPath(AppSettings.Default.GeneralSection.CustomSongDir);
        if (dbFolder.StartsWith(customFolder) || customFolder.StartsWith(customFolder))
        {
            return;
        }

        lock (ScanObject)
        {
            if (ViewModel.IsScanning)
                return;
            ViewModel.IsScanning = true;
        }

        _scanCts = new CancellationTokenSource();
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        await dbContext.RemoveFolderAll();
        var dirInfo = new DirectoryInfo(path);
        var concurrentBag = new ConcurrentDictionary<string, PlayItemDetail>();

        if (dirInfo.Exists)
        {
            await Task.Run(() =>
            {
                dirInfo.EnumerateDirectories(searchPattern: "*.*", searchOption: SearchOption.TopDirectoryOnly)
                    .AsParallel()
                    .ForAll(privateFolder =>
                    {
                        ScanPrivateFolder(concurrentBag, privateFolder);
                    });
            });
        }

        await SynchronizeManaged(concurrentBag);

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

    private void ScanPrivateFolder(ConcurrentDictionary<string, PlayItemDetail> playItemDetails, DirectoryInfo privateFolder)
    {
        foreach (var fileInfo in privateFolder.EnumerateFiles("*.osu", SearchOption.AllDirectories))
        {
            if (_scanCts.IsCancellationRequested)
                return;
            try
            {
                var osuFile = OsuFile.ReadFromFile(fileInfo.FullName,
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
                var playItemDetail = new PlayItemDetail();
                UpdateDetailByCoosu(playItemDetail, osuFile);
                var songFolder = AppSettings.Default.GeneralSection.OsuSongDir;
                var fullPath = Path.GetFullPath(songFolder);
                var index = fileInfo.FullName.IndexOf(fullPath, StringComparison.Ordinal);
                string standardizedPath;
                var separator = Path.DirectorySeparatorChar;
                if (index == 0)
                {
                    var subStr = fileInfo.FullName.Substring(fullPath.Length);
                    standardizedPath = "./" + subStr.Replace(separator, '/');
                    //var index = fileInfo.FullName;
                }
                else
                {
                    standardizedPath = fileInfo.FullName.Replace(separator, '/');
                }

                playItemDetails.TryAdd(standardizedPath, playItemDetail);
            }
            catch (Exception ex)
            {
                LogTo.ErrorException($"Error during scanning file, ignored {fileInfo.FullName}", ex);
            }
        }
    }

    public async ValueTask SynchronizeManaged(ConcurrentDictionary<string, PlayItemDetail> fromCustomFolder)
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var sw = Stopwatch.StartNew();
        var dbItems = await dbContext.PlayItems
            .Include(k => k.PlayItemDetail)
            .Where(k => k.IsAutoManaged && !k.StandardizedPath.StartsWith("./"))
            .ToDictionaryAsync(k => k.StandardizedPath, k => k);

        var maxDetailId = dbItems.Values.Count == 0 ? 0 : dbItems.Values.Max(k => k.PlayItemDetail.Id);
        maxDetailId++;

        LogTo.Debug(() => $"Found {dbItems.Count} items in {sw.ElapsedMilliseconds}ms.");
        sw.Restart();

        // Delete obsolete
        var obsoleteNeedDel = dbItems
            .Where(k => !fromCustomFolder.ContainsKey(k.Key))
            .Select(k => k.Value)
            .ToList();

        LogTo.Debug(() => $"Found {obsoleteNeedDel.Count} items to delete in {sw.ElapsedMilliseconds}ms.");
        sw.Restart();
        if (obsoleteNeedDel.Count > 0)
        {
            await dbContext.BulkDeleteAsync(obsoleteNeedDel);
            await dbContext.BulkSaveChangesAsync();

            LogTo.Debug(() => $"Delete {dbItems.Count} items in {sw.ElapsedMilliseconds}ms.");
            sw.Restart();
        }

        // Update exist
        var existNeedUpdate = dbItems
            .Select((k, i) =>
            {
                if (!fromCustomFolder.TryGetValue(k.Key, out var newDetail)) return null!;
                var oldDetial = k.Value.PlayItemDetail;
                oldDetial.FolderName = newDetail.FolderName;
                oldDetial.Artist = newDetail.Artist;
                oldDetial.ArtistUnicode = newDetail.ArtistUnicode;
                oldDetial.Title = newDetail.Title;
                oldDetial.TitleUnicode = newDetail.TitleUnicode;
                oldDetial.Creator = newDetail.Creator;
                oldDetial.Version = newDetail.Version;

                oldDetial.BeatmapFileName = newDetail.BeatmapFileName;
                //oldDetial.LastModified = newDetail.LastModified;
                oldDetial.DefaultStarRatingStd = newDetail.DefaultStarRatingStd;
                oldDetial.DefaultStarRatingTaiko = newDetail.DefaultStarRatingTaiko;
                oldDetial.DefaultStarRatingCtB = newDetail.DefaultStarRatingCtB;
                oldDetial.DefaultStarRatingMania = newDetail.DefaultStarRatingMania;
                //oldDetial.DrainTime = newDetail.DrainTime;
                oldDetial.TotalTime = newDetail.TotalTime;
                //oldDetial.AudioPreviewTime = newDetail.AudioPreviewTime;
                oldDetial.BeatmapId = newDetail.BeatmapId;
                oldDetial.BeatmapSetId = newDetail.BeatmapSetId;
                oldDetial.GameMode = newDetail.GameMode;
                oldDetial.Source = newDetail.Source;
                oldDetial.Tags = newDetail.Tags;
                oldDetial.FolderName = PathUtils.GetFolder(k.Key);
                oldDetial.AudioFileName = newDetail.AudioFileName;
                return newDetail;
            })
            .Where(k => k != null!)
            .ToArray();

        LogTo.Debug(() => $"Found {existNeedUpdate.Length} items to update in {sw.ElapsedMilliseconds}ms.");
        sw.Restart();

        if (existNeedUpdate.Length > 0)
        {
            var actualUpdated = await dbContext.SaveChangesAsync();
            LogTo.Debug(() => $"Update {actualUpdated} items in {sw.ElapsedMilliseconds}ms.");
            sw.Restart();
        }

        // Add new
        var listDetail = new List<PlayItemDetail>();
        var listItem = new List<PlayItem>();
        foreach (var playItemDetail in fromCustomFolder.Where(k => !dbItems.ContainsKey(k.Key)))
        {
            playItemDetail.Value.Id = maxDetailId++;
            listDetail.Add(playItemDetail.Value);

            var path = playItemDetail.Key;
            var folder = PathUtils.GetFolder(path);
            playItemDetail.Value.FolderName = folder;
            listItem.Add(new PlayItem
            {
                IsAutoManaged = true,
                StandardizedPath = path,
                StandardizedFolder = folder,
                PlayItemDetailId = playItemDetail.Value.Id
            });
        }

        LogTo.Debug(() => $"Found {listItem.Count} items to Add in {sw.ElapsedMilliseconds}ms.");
        sw.Restart();

        if (listItem.Count > 0)
        {
            await dbContext.BulkInsertAsync(listDetail);
            await dbContext.BulkSaveChangesAsync();
            await dbContext.BulkInsertAsync(listItem);
            await dbContext.BulkSaveChangesAsync();

            LogTo.Debug(() => $"Add {listItem.Count} items in {sw.ElapsedMilliseconds}ms.");
            sw.Restart();
        }
    }

    private static void UpdateDetailByCoosu(PlayItemDetail playItemDetail, LocalOsuFile osuFile)
    {
        playItemDetail.Artist = osuFile.Metadata?.Artist ?? "";
        playItemDetail.ArtistUnicode = osuFile.Metadata?.ArtistUnicode ?? "";
        playItemDetail.Title = osuFile.Metadata?.Title ?? "";
        playItemDetail.TitleUnicode = osuFile.Metadata?.TitleUnicode ?? "";
        playItemDetail.Creator = osuFile.Metadata?.Creator ?? "";
        playItemDetail.Version = osuFile.Metadata?.Version ?? "";

        playItemDetail.BeatmapFileName = Path.GetFileName(osuFile.OriginalPath)!;
        playItemDetail.GameMode = osuFile.General?.Mode ?? GameMode.Circle;

        if (osuFile.HitObjects != null)
        {
            playItemDetail.TotalTime = TimeSpan.FromMilliseconds(osuFile.HitObjects.MaxTime);
        }

        playItemDetail.BeatmapId = osuFile.Metadata?.BeatmapId ?? -1;
        playItemDetail.BeatmapSetId = osuFile.Metadata?.BeatmapSetId ?? -1;
        playItemDetail.Source = osuFile.Metadata?.Source ?? "";
        playItemDetail.Tags = osuFile.Metadata == null ? "" : string.Join(" ", osuFile.Metadata.TagList);
        playItemDetail.AudioFileName = osuFile.General!.AudioFilename ?? "";
        playItemDetail.FolderName = Path.GetFileName(Path.GetDirectoryName(osuFile.OriginalPath)) ?? "";
        //throw new NotImplementedException("Determine whether osuFile is from song folder");
        //playItemDetail.FolderName = Path.GetDirectoryName(osuFile.OriginalPath)!;
    }
}