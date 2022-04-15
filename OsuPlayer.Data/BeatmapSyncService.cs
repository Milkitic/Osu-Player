using Anotar.NLog;
using Microsoft.EntityFrameworkCore;
using OsuPlayer.Data.Models;
using OsuPlayer.Shared;
using OsuPlayer.Shared.Configuration;

namespace OsuPlayer.Data;

public class BeatmapSyncService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AppSettings _configuration;

    public BeatmapSyncService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        _configuration = ConfigurationFactory.GetConfiguration<AppSettings>();
    }

    public async ValueTask SynchronizeManaged(IEnumerable<PlayItemDetail> fromDb)
    {
        if (_configuration.Data.OsuBaseFolder == null)
        {
            throw new ArgumentNullException(nameof(SectionData.OsuBaseFolder), default(string));
        }

        var dbItems = await _dbContext.PlayItems
            .AsNoTracking()
            .Where(k => k.IsAutoManaged)
            .ToDictionaryAsync(k => k.Path, k => (k.Id, k.CachedInfoId));

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