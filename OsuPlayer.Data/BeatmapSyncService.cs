using Anotar.NLog;
using Microsoft.EntityFrameworkCore;
using OsuPlayer.Data.Models;

namespace OsuPlayer.Data;

public class BeatmapSyncService
{
    private readonly ApplicationDbContext _dbContext;

    public BeatmapSyncService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask SynchronizeManaged(IEnumerable<PlayItemDetail> fromDb)
    {
        var dbItems = await _dbContext.PlayItems
            .AsNoTracking()
            .Where(k => k.IsAutoManaged)
            .ToDictionaryAsync(k => k.Path, k => (k.Id, CachedInfoId: k.PlayItemDetailId));

        LogTo.Debug(() => $"Sync: found {dbItems.Count} items.");

        var newAllPaths = fromDb
            .ToDictionary(k => Path.Combine(k.FolderName, k.BeatmapFileName), k => k);

        // Delete obsolete
        var obsoleteNeedDel = dbItems
            .Where(k => !newAllPaths.ContainsKey(k.Key))
            .Select(k => k.Value.Id)
            .ToHashSet();

        LogTo.Debug(() => $"Sync: found {obsoleteNeedDel.Count} items to delete.");

        _dbContext.PlayItems.RemoveRange(_dbContext.PlayItems.Where(k => obsoleteNeedDel.Contains(k.Id)));

        // Update exist
        var existNeedUpdate = dbItems
            .Select(k =>
            {
                if (newAllPaths.TryGetValue(k.Key, out var newDetails))
                {
                    newDetails.Id = k.Value.CachedInfoId;
                    return newDetails;
                }

                return null!;
            })
            .Where(k => k != null!)
            .ToArray();

        LogTo.Debug(() => $"Sync: found {existNeedUpdate.Length} items to update.");

        if (existNeedUpdate.Length > 0)
        {
            _dbContext.PlayItemDetails.UpdateRange(existNeedUpdate);
        }

        // Add new
        var newNeedAdd = newAllPaths
            .Where(k => !dbItems.ContainsKey(k.Key))
            .Select(k => new PlayItem
            {
                IsAutoManaged = true,
                Path = k.Key,
                PlayItemDetail = k.Value
            })
            .ToArray();

        LogTo.Debug(() => $"Sync: found {newNeedAdd.Length} items to Add.");

        _dbContext.PlayItems.AddRange(newNeedAdd);

        await _dbContext.SaveChangesAsync();
    }
}