using System.Diagnostics;
using System.IO;
using Anotar.NLog;
using Coosu.Database;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Data.Internal;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Utils;

namespace Milki.OsuPlayer.Services;

public class BeatmapSyncService
{
    public IEnumerable<PlayItemDetail> EnumeratePlayItemDetailsFormDb(string path)
    {
        using var reader = new OsuDbReader(path);
        var beatmaps = reader.EnumerateDbModels();
        return beatmaps;
    }

    public async ValueTask SynchronizeManaged(IEnumerable<PlayItemDetail> fromOsuDb)
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();

        var sw = Stopwatch.StartNew();
        var dbItems = await dbContext.PlayItems
            .Include(k => k.PlayItemDetail)
            .Where(k => k.IsAutoManaged && k.StandardizedPath.StartsWith("./"))
            .ToDictionaryAsync(k => k.StandardizedPath, k => k);

        var maxDetailId = dbItems.Values.Count == 0 ? 0 : dbItems.Values.Max(k => k.PlayItemDetail.Id);
        maxDetailId++;

        LogTo.Debug(() => $"Found {dbItems.Count} items in {sw.ElapsedMilliseconds}ms.");
        sw.Restart();

        var newAllPaths = fromOsuDb
            .Select(k =>
            {
                var separator = Path.DirectorySeparatorChar;
                var finalPath = string.Create(k.FolderName.Length + k.BeatmapFileName.Length + 3, k, (span, s) =>
                {
                    span[0] = '.';
                    span[1] = '/';
                    var folder = s.FolderName.AsSpan();
                    int i = 2;
                    foreach (var c in folder)
                    {
                        span[i] = c == separator ? '/' : c;
                        i++;
                    }

                    span[i] = '/';
                    s.BeatmapFileName.CopyTo(span[(i + 1)..]);
                });

                return new KeyValuePair<string, PlayItemDetail>(finalPath, k);
            })
            .Distinct(StringKeyComparer.Instance)
            .ToDictionary(k => k.Key, k => k.Value);

        LogTo.Debug(() => $"Enumerate {dbItems.Count} items in osu!db in {sw.ElapsedMilliseconds}ms.");
        sw.Restart();

        // Delete obsolete
        var obsoleteNeedDel = dbItems
            .Where(k => !newAllPaths.ContainsKey(k.Key))
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
                if (!newAllPaths.TryGetValue(k.Key, out var newDetail)) return null!;
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
        foreach (var playItemDetail in newAllPaths.Where(k => !dbItems.ContainsKey(k.Key)))
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
}