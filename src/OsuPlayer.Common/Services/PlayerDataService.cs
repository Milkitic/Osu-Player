using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared.Models;

namespace Milky.OsuPlayer.Services
{
    public sealed class PlayerDataService : IPlayerDataStore
    {
        private readonly Func<OsuPlayerDbContext> _createDbContext;

        public PlayerDataService()
            : this(() => new OsuPlayerDbContext())
        {
        }

        public PlayerDataService(Func<OsuPlayerDbContext> createDbContext)
        {
            _createDbContext = createDbContext ?? throw new ArgumentNullException(nameof(createDbContext));
        }

        public async Task<Beatmap> GetBeatmapByIdentifiableAsync(IMapIdentifiable beatmap)
        {
            await using var db = _createDbContext();
            return await db.GetBeatmapByIdentifiableAsync(beatmap);
        }

        public async Task<BeatmapSettings> GetMapFromDbAsync(IMapIdentifiable beatmap)
        {
            await using var db = _createDbContext();
            return await db.GetMapFromDbAsync(beatmap);
        }

        public async Task<bool> TryRemoveFromRecentAsync(MapIdentity identity)
        {
            await using var db = _createDbContext();
            await db.RemoveFromRecentAsync(identity);
            return true;
        }

        public async Task<bool> TryRemoveMapFromCollectionAsync(IMapIdentifiable identity, Collection collection)
        {
            await using var db = _createDbContext();
            await db.RemoveMapFromCollectionAsync(identity, collection);
            return true;
        }

        public async Task<PaginationQueryResult<Beatmap>> SearchBeatmapPageAsync(string searchText,
            BeatmapSortMode sortMode, int startIndex, int count)
        {
            await using var db = _createDbContext();
            return await db.SearchBeatmapPageAsync(searchText, sortMode, startIndex, count);
        }

        public async Task<List<Beatmap>> SearchBeatmapByOptionsAsync(string searchText, BeatmapSortMode sortMode,
            int startIndex,
            int count)
        {
            await using var db = _createDbContext();
            return await db.SearchBeatmapByOptionsAsync(searchText, sortMode, startIndex, count);
        }

        public async Task<List<Beatmap>> GetBeatmapsFromFolderAsync(string folderName)
        {
            await using var db = _createDbContext();
            return await db.GetBeatmapsFromFolderAsync(folderName);
        }

        public async Task<List<Collection>> GetCollectionsAsync()
        {
            await using var db = _createDbContext();
            return await db.GetCollectionsAsync();
        }

        public async Task<List<Collection>> GetCollectionsByMapAsync(BeatmapSettings beatmapSettings)
        {
            await using var db = _createDbContext();
            return await db.GetCollectionsByMapAsync(beatmapSettings);
        }

        public async Task<bool> TryAddCollectionAsync(string collectionName)
        {
            await using var db = _createDbContext();
            await db.AddCollectionAsync(collectionName);
            return true;
        }

        public async Task<List<Beatmap>> GetBeatmapsByIdentifiableAsync(IEnumerable<IMapIdentifiable> mapIdentities)
        {
            await using var db = _createDbContext();
            return await db.GetBeatmapsByIdentifiableAsync(mapIdentities);
        }

        public async Task<bool> TryUpdateCollectionAsync(Collection collection)
        {
            await using var db = _createDbContext();
            await db.UpdateCollectionAsync(collection);
            return true;
        }

        public async Task<bool> TryUpdateMapAsync(IMapIdentifiable beatmap, int? offset = null)
        {
            await using var db = _createDbContext();
            await db.UpdateMapAsync(beatmap, offset);
            return true;
        }

        public async Task<Collection> GetCollectionByIdAsync(string id)
        {
            await using var db = _createDbContext();
            return await db.GetCollectionByIdAsync(id);
        }

        public async Task<List<BeatmapSettings>> GetMapsFromCollectionAsync(Collection collection)
        {
            await using var db = _createDbContext();
            return await db.GetMapsFromCollectionAsync(collection);
        }

        public async Task<List<Beatmap>> GetBeatmapsByMapInfoAsync(List<BeatmapSettings> settings,
            TimeSortMode sortMode)
        {
            await using var db = _createDbContext();
            return await db.GetBeatmapsByMapInfoAsync(settings, sortMode);
        }

        public async Task<bool> TryRemoveCollectionAsync(Collection collection)
        {
            await using var db = _createDbContext();
            await db.RemoveCollectionAsync(collection);
            return true;
        }

        public async Task<bool> TryAddMapExportAsync(IMapIdentifiable mapIdentity, string path)
        {
            await using var db = _createDbContext();
            await db.AddMapExportAsync(mapIdentity, path);
            return true;
        }

        public async Task<List<BeatmapSettings>> GetRecentListAsync()
        {
            await using var db = _createDbContext();
            return await db.GetRecentListAsync();
        }

        public async Task<List<BeatmapSettings>> GetExportedMapsAsync()
        {
            await using var db = _createDbContext();
            return await db.GetExportedMapsAsync();
        }

        public async Task<bool> TryClearRecentAsync()
        {
            await using var db = _createDbContext();
            await db.ClearRecentAsync();
            return true;
        }

        public async Task<bool> TryAddMapsToCollectionAsync(IList<Beatmap> beatmaps, Collection collection)
        {
            await using var db = _createDbContext();
            await db.AddMapsToCollectionAsync(beatmaps, collection);
            return true;
        }

        public async Task<bool> TryRemoveLocalAllAsync()
        {
            await using var db = _createDbContext();
            await db.RemoveLocalAllAsync();
            return true;
        }

        public async Task<bool> TryAddNewMapsAsync(IEnumerable<Beatmap> beatmaps)
        {
            await using var db = _createDbContext();
            await db.AddNewMapsAsync(beatmaps);
            return true;
        }

        public async Task SyncMapsFromOsuDbAsync(IEnumerable<Beatmap> beatmaps, bool addOnly)
        {
            await using var db = _createDbContext();
            await db.SyncMapsFromOsuDbAsync(beatmaps, addOnly);
        }

        public async Task<(bool found, string thumbPath)> TryGetMapThumbAsync(Guid beatmapDbId)
        {
            await using var db = _createDbContext();
            return await db.GetMapThumbAsync(beatmapDbId);
        }

        public async Task<bool> TrySetMapThumbAsync(Guid beatmapDbId, string thumbPath)
        {
            await using var db = _createDbContext();
            await db.SetMapThumbAsync(beatmapDbId, thumbPath);
            return true;
        }
    }
}