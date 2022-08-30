﻿using System.Diagnostics;
using System.Linq.Expressions;
using Anotar.NLog;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Data;

public sealed partial class ApplicationDbContext
{
    public async ValueTask<PaginationQueryResult<PlayGroupQuery>> SearchPlayItemsAsync(string searchText,
        BeatmapOrderOptions beatmapOrderOptions,
        int page,
        int countPerPage)
    {
        if (page <= 0) page = 1;

        var array = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        IQueryable<PlayItemDetail> playItemDetails = PlayItemDetails;
        foreach (var s in array)
        {
            playItemDetails = playItemDetails.Where(GetWhereExpression(s));
        }

        var query = PlayItems
            .AsNoTracking()
            .Include(k => k.PlayItemAsset)
            .Include(k => k.PlayItemDetail)
            .Join(playItemDetails,
                playItem => playItem.PlayItemDetailId,
                playItemDetail => playItemDetail.Id,
                (playItem, playItemDetail) => new
                {
                    PlayItem = playItem,
                    PlayItemDetail = playItemDetail,
                    PlayItemAssets = playItem.PlayItemAsset ?? new PlayItemAsset()
                })
            .Select(k => new PlayGroupQuery
            {
                Folder = k.PlayItem.StandardizedFolder,
                IsAutoManaged = k.PlayItem.IsAutoManaged,
                Artist = k.PlayItemDetail.Artist,
                ArtistUnicode = k.PlayItemDetail.ArtistUnicode,
                Title = k.PlayItemDetail.Title,
                TitleUnicode = k.PlayItemDetail.TitleUnicode,
                Tags = k.PlayItemDetail.Tags,
                Source = k.PlayItemDetail.Source,
                Creator = k.PlayItemDetail.Creator,
                BeatmapSetId = k.PlayItemDetail.BeatmapSetId,
                //ThumbPath = k.PlayItemAssets.ThumbPath,
                //StoryboardVideoPath = k.PlayItemAssets.StoryboardVideoPath,
                //VideoPath = k.PlayItemAssets.VideoPath,
                StarRating = k.PlayItemDetail.StarRating,
                CurrentPlayItem = k.PlayItem,
                CurrentPlayItemDetail = k.PlayItemDetail,
            });

        var sqlStr = query.ToQueryString();
        var fullResult = await query.ToArrayAsync();

        var enumerable = fullResult
            .GroupBy(k => k.Folder, StringComparer.Ordinal)
            .SelectMany(k => k
                .GroupBy(o => o, MetaComparer.Instance)
                .Select(grouping =>
                {
                    var playGroupQuery = grouping.OrderByDescending(groupQuery => groupQuery.StarRating).First();
                    playGroupQuery.GroupPlayItems = grouping
                        .GroupBy(groupQuery => groupQuery.CurrentPlayItem.PlayItemDetail.GameMode)
                        .OrderBy(groupQuery => groupQuery.Key)
                        .ToDictionary(modeGrouping => modeGrouping.Key, modeGrouping => modeGrouping
                            .OrderBy(groupQuery => groupQuery.StarRating)
                            .Select(groupQuery => groupQuery.CurrentPlayItem)
                            .ToArray()
                        );
                    playGroupQuery.CurrentPlayItem.PlayItemAsset ??= new PlayItemAsset();
                    return playGroupQuery;
                })
            );

        enumerable = beatmapOrderOptions switch
        {
            BeatmapOrderOptions.Artist => enumerable.OrderBy(k =>
                    string.IsNullOrEmpty(k.ArtistUnicode) ? k.Artist : k.ArtistUnicode,
                StringComparer.InvariantCultureIgnoreCase),
            BeatmapOrderOptions.Title => enumerable.OrderBy(k =>
                    string.IsNullOrEmpty(k.TitleUnicode) ? k.Title : k.TitleUnicode,
                StringComparer.InvariantCultureIgnoreCase),
            BeatmapOrderOptions.Creator => enumerable.OrderBy(k => k.Creator,
                StringComparer.OrdinalIgnoreCase),
            _ => throw new ArgumentOutOfRangeException(nameof(beatmapOrderOptions), beatmapOrderOptions, null)
        };

        var bufferResult = enumerable.ToArray();
        var totalCount = bufferResult.Length;
        var beatmaps = bufferResult.Skip((page - 1) * countPerPage).Take(countPerPage).ToArray();

        return new PaginationQueryResult<PlayGroupQuery>(beatmaps, totalCount);
    }

    public async ValueTask<PlayList[]> GetPlayListsAsync()
    {
        return await PlayLists
            .AsNoTracking()
            .OrderByDescending(k => k.Index)
            .ToArrayAsync();
    }

    public async ValueTask<PaginationQueryResult<PlayItem>> GetPlayItemsFromPlayListAsync(PlayList playList,
        int page = 0,
        int countPerPage = 50,
        BeatmapOrderOptions options = BeatmapOrderOptions.CreateTime)
    {
        if (playList == null) throw new ArgumentNullException(nameof(playList));
        var relations = PlayListRelations
            .AsNoTracking()
            .Where(k => k.PlayListId == playList.Id);

        var ordered = options switch
        {
            BeatmapOrderOptions.Index => relations.OrderByDescending(k => k.Index),
            BeatmapOrderOptions.CreateTime => relations.OrderByDescending(k => k.CreateTime),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options, null)
        };

        var beatmaps = ordered.Join(PlayItems,
            k => k.PlayItemId,
            k => k.Id,
            (k, x) => x);

        var buffer = await beatmaps.ToArrayAsync();
        var count = buffer.Length;
        var result = buffer.Skip(page * countPerPage).Take(countPerPage).ToArray();

        return new PaginationQueryResult<PlayItem>(result, count);
    }


    public async Task AddOrUpdatePlayListAsync(PlayList playList)
    {
        var result = await PlayLists.FindAsync(playList.Id);
        if (result == null)
        {
            await AddPlayListAsync(playList.Name);
        }
        else
        {
            result.Description = playList.Description;
            result.ImagePath = playList.ImagePath;
            result.Name = playList.Name;

            await SaveChangesAsync();
        }
    }

    public async ValueTask AddPlayListAsync(string name, bool locked = false)
    {
        var maxIndex = await PlayLists
            .OrderByDescending(k => k.Index)
            .Select(k => k.Index)
            .FirstOrDefaultAsync();
        var collection = new PlayList
        {
            Name = name,
            IsDefault = locked,
            Index = maxIndex + 1,
        };

        PlayLists.Add(collection);
        await SaveChangesAsync();
    }

    public async ValueTask AddPlayItemsToPlayList(IList<PlayItem> playItems, PlayList collection)
    {
        if (playItems.Count < 1) return;
        var findAsync = await PlayLists.FindAsync(collection.Id);
        if (findAsync == null) return;
        await Entry(findAsync).Collection(p => p.PlayItems).LoadAsync();
        findAsync.PlayItems.AddRange(playItems);

        await SaveChangesAsync();
    }

    public async ValueTask DeletePlayItemsFromPlayListAsync(IEnumerable<PlayItem> playItems, PlayList playList)
    {
        var relations = playItems
            .Join(PlayListRelations.Where(k => k.PlayListId == playList.Id), k => k.Id,
                k => k.PlayItemId,
                (_, k) => k)
            .ToArray();

        await this.BulkDeleteAsync(relations);
        await this.BulkSaveChangesAsync();
    }

    public async ValueTask<List<LoosePlayItem>> GetCurrentListFull()
    {
        var query =
            from looseItem in LoosePlayItems
            where looseItem.LooseItemType == LooseItemType.CurrentPlay
            join playItem in PlayItems on looseItem.PlayItemId equals playItem.Id into newCollection
            from playItem in newCollection.DefaultIfEmpty()
            select new
            {
                looseItem,
                playItem,
            };

        var buffer = new List<LoosePlayItem>();
        await foreach (var item in query.AsAsyncEnumerable())
        {
            item.looseItem.PlayItem = item.playItem;
            buffer.Add(item.looseItem);
        }

        return buffer;
    }

    public async ValueTask<PaginationQueryResult<LoosePlayItem>> GetRecentListFull(
        int page = 0,
        int countPerPage = 50)
    {
        var query =
            from looseItem in LoosePlayItems
            where looseItem.LooseItemType == LooseItemType.RecentPlay
            orderby looseItem.LastPlay descending
            join playItem in PlayItems.Include(k => k.PlayItemDetail)/*.Include(k => k.PlayItemAsset)*/ on looseItem.PlayItemId equals playItem.Id into newCollection
            from playItem in newCollection.DefaultIfEmpty()
            select new
            {
                looseItem,
                playItem,
            };
        //var asyncEnumerable = RecentPlay
        //    .AsNoTracking()
        //    .OrderByDescending(k => k.LastPlay)
        //    .Join(PlayItems, k => k.PlayItemId, k => k.Id, (looseItem, playItem) => new
        //    {
        //        looseItem,
        //        playItem
        //    })
        //    .AsAsyncEnumerable();
        var buffer = new List<LoosePlayItem>();
        await foreach (var item in query.AsAsyncEnumerable())
        {
            item.looseItem.PlayItem = item.playItem;
            buffer.Add(item.looseItem);
        }

        var count = buffer.Count;
        var result = buffer
            .Skip(page * countPerPage)
            .Take(countPerPage)
            .ToArray();

        return new PaginationQueryResult<LoosePlayItem>(result, count);
    }

    public async ValueTask<PaginationQueryResult<LoosePlayItem>> GetRecentList(
        int page = 0,
        int countPerPage = 50)
    {
        var buffer = await LoosePlayItems
            .AsNoTracking()
            .OrderByDescending(k => k.LastPlay)
            .ToArrayAsync();

        var count = buffer.Length;
        var result = buffer
            .Skip(page * countPerPage)
            .Take(countPerPage)
            .ToArray();

        return new PaginationQueryResult<LoosePlayItem>(result, count);
    }

    public async ValueTask ClearRecentList()
    {
        LoosePlayItems.RemoveRange(LoosePlayItems.Where(k => k.LooseItemType == LooseItemType.RecentPlay));
        await SaveChangesAsync();
    }

    public async ValueTask AddOrUpdatePlayItemToRecentPlayAsync(PlayItem playItem, DateTime playTime)
    {
        await AddOrUpdateLoosePlayItemCore(playItem, playTime, LoosePlayItems, LooseItemType.RecentPlay);
    }

    public async ValueTask AddOrUpdatePlayItemToCurrentPlayAsync(PlayItem playItem, DateTime playTime)
    {
        await AddOrUpdateLoosePlayItemCore(playItem, playTime, LoosePlayItems, LooseItemType.CurrentPlay);
    }

    public async ValueTask RecreateCurrentPlayAsync(IEnumerable<PlayItem> playItems)
    {
        var sw = Stopwatch.StartNew();
        var dbItems = await LoosePlayItems
            .Where(k => k.LooseItemType == LooseItemType.CurrentPlay)
            .ToDictionaryAsync(k => k.PlayItemId, k => k);

        LogTo.Debug(() => $"Found {dbItems.Count} LooseItems in {sw.ElapsedMilliseconds}ms.");
        sw.Restart();

        var newAllLooseItems = playItems
            .Select(k => new KeyValuePair<int, LoosePlayItem>(k.Id, k.ToLoosePlayItem(DateTime.MinValue, LooseItemType.CurrentPlay)))
            .ToDictionary(k => k.Key, k => k.Value);

        LogTo.Debug(() => $"Enumerate {dbItems.Count} newAllLooseItems in {sw.ElapsedMilliseconds}ms.");
        sw.Restart();

        // Delete obsolete
        var obsoleteNeedDel = dbItems
            .Where(k => !newAllLooseItems.ContainsKey(k.Key.Value))
            .Select(k => k.Value)
            .ToList();

        LogTo.Debug(() => $"Found {obsoleteNeedDel.Count} LooseItems to delete in {sw.ElapsedMilliseconds}ms.");
        sw.Restart();
        if (obsoleteNeedDel.Count > 0)
        {
            await this.BulkDeleteAsync(obsoleteNeedDel);
            await this.BulkSaveChangesAsync();

            LogTo.Debug(() => $"Delete {dbItems.Count} LooseItems in {sw.ElapsedMilliseconds}ms.");
            sw.Restart();
        }

        // Update exist
        var existNeedUpdate = dbItems
            .Select((k, i) =>
            {
                if (!newAllLooseItems.TryGetValue(k.Key.Value, out var newItem)) return null!;
                var oldItem = k.Value;
                oldItem.Artist = newItem.Artist;
                oldItem.Title = newItem.Title;
                oldItem.Creator = newItem.Creator;
                oldItem.Version = newItem.Version;
                oldItem.Index = newItem.Index;
                return newItem;
            })
            .Where(k => k != null!)
            .ToArray();

        LogTo.Debug(() => $"Found {existNeedUpdate.Length} LooseItems to update in {sw.ElapsedMilliseconds}ms.");
        sw.Restart();

        if (existNeedUpdate.Length > 0)
        {
            var actualUpdated = await this.SaveChangesAsync();
            LogTo.Debug(() => $"Update {actualUpdated} LooseItems in {sw.ElapsedMilliseconds}ms.");
            sw.Restart();
        }

        // Add new
        var listItem = newAllLooseItems
            .Where(k => !dbItems.ContainsKey(k.Key))
            .Select(playItemDetail => playItemDetail.Value)
            .ToList();

        LogTo.Debug(() => $"Found {listItem.Count} LooseItems to Add in {sw.ElapsedMilliseconds}ms.");
        sw.Restart();

        if (listItem.Count > 0)
        {
            await this.BulkInsertAsync(listItem/*, k => k.CustomDestinationTableName = nameof(CurrentPlay)*/);
            await this.BulkSaveChangesAsync();

            LogTo.Debug(() => $"Add {listItem.Count} LooseItems in {sw.ElapsedMilliseconds}ms.");
            sw.Restart();
        }
    }

    public async Task<PaginationQueryResult<ExportItem>> GetExportListFull(
        int page = 0,
        int countPerPage = 50)
    {
        var query =
            from exportItem in Exports
            orderby exportItem.ExportTime descending
            join playItem in PlayItems on exportItem.PlayItemId equals playItem.Id into newCollection
            from playItem in newCollection.DefaultIfEmpty()
            select new
            {
                exportItem,
                playItem,
            };

        var buffer = new List<ExportItem>();
        await foreach (var item in query.AsAsyncEnumerable())
        {
            item.exportItem.PlayItem = item.playItem;
            buffer.Add(item.exportItem);
        }

        var count = buffer.Count;
        var result = buffer
            .Skip(page * countPerPage)
            .Take(countPerPage)
            .ToArray();

        return new PaginationQueryResult<ExportItem>(result, count);
    }

    public async Task AddOrUpdateExportAsync(ExportItem export)
    {
        var exist = await Exports.FindAsync(export.Id);
        if (exist != null)
        {
            exist.Size = exist.Size;
            exist.ExportPath = export.ExportPath;
            exist.ExportTime = export.ExportTime;
            exist.Title = export.Title;
            exist.Artist = export.Artist;
            exist.Creator = export.Creator;
            exist.Version = export.Version;
            exist.PlayItemStandardizedPath = export.PlayItemStandardizedPath;
            exist.PlayItemId = export.PlayItemId;
        }
        else
        {
            Exports.Add(export);
        }

        await SaveChangesAsync();
    }

    public async Task<PaginationQueryResult<ExportItem>> GetExportList(
        int page = 0,
        int countPerPage = 50)
    {
        var buffer = await Exports
            .AsNoTracking()
            .OrderByDescending(k => k.ExportTime)
            .ToArrayAsync();

        var count = buffer.Length;

        var result = buffer
            .Skip(page * countPerPage)
            .Take(countPerPage)
            .ToArray();

        return new PaginationQueryResult<ExportItem>(result, count);
    }

    private async ValueTask AddOrUpdateLoosePlayItemCore(PlayItem playItem, DateTime playTime,
        DbSet<LoosePlayItem> loosePlayItems, LooseItemType looseItemType)
    {
        var loosePlayItem = await loosePlayItems
            .FirstOrDefaultAsync(k => k.PlayItemId == playItem.Id);
        if (loosePlayItem == null)
        {
            loosePlayItems.Add(playItem.ToLoosePlayItem(playTime, looseItemType));
        }
        else
        {
            loosePlayItem.UpdateFromPlayItem(playItem, playTime);
        }

        await SaveChangesAsync();
    }

    private static Expression<Func<PlayItemDetail, bool>> GetWhereExpression(string searchText)
    {
        var text = $"%{searchText}%";
        return k =>
            EF.Functions.Like(k.Artist, text) ||
            EF.Functions.Like(k.ArtistUnicode, text) ||
            EF.Functions.Like(k.Title, text) ||
            EF.Functions.Like(k.TitleUnicode, text) ||
            EF.Functions.Like(k.Tags, text) ||
            EF.Functions.Like(k.Source, text) ||
            EF.Functions.Like(k.Creator, text) ||
            EF.Functions.Like(k.Version, text);
    }
}