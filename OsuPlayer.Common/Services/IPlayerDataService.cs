using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared.Models;

namespace Milky.OsuPlayer.Services
{
    public interface IPlayerDataService : IPlayerDataStore
    {
    }

    public interface IPlayerDataStore
    {
        Task<Beatmap> GetBeatmapByIdentifiableAsync(IMapIdentifiable beatmap);

        Task<BeatmapSettings> GetMapFromDbAsync(IMapIdentifiable beatmap);

        Task<bool> TryRemoveFromRecentAsync(MapIdentity identity);

        Task<bool> TryRemoveMapFromCollectionAsync(IMapIdentifiable identity, Collection collection);

        Task<List<Beatmap>> SearchBeatmapByOptionsAsync(string searchText, BeatmapSortMode sortMode, int startIndex,
            int count);

        Task<List<Beatmap>> GetBeatmapsFromFolderAsync(string folderName);

        Task<List<Collection>> GetCollectionsAsync();

        Task<List<Collection>> GetCollectionsByMapAsync(BeatmapSettings beatmapSettings);

        Task<bool> TryAddCollectionAsync(string collectionName);

        Task<List<Beatmap>> GetBeatmapsByIdentifiableAsync(IEnumerable<IMapIdentifiable> mapIdentities);

        Task<bool> TryUpdateCollectionAsync(Collection collection);

        Task<bool> TryUpdateMapAsync(IMapIdentifiable beatmap, int? offset = null);

        Task<Collection> GetCollectionByIdAsync(string id);

        Task<List<BeatmapSettings>> GetMapsFromCollectionAsync(Collection collection);

        Task<List<Beatmap>> GetBeatmapsByMapInfoAsync(List<BeatmapSettings> settings, TimeSortMode sortMode);

        Task<bool> TryRemoveCollectionAsync(Collection collection);

        Task<bool> TryAddMapExportAsync(IMapIdentifiable mapIdentity, string path);

        Task<List<BeatmapSettings>> GetRecentListAsync();

        Task<List<BeatmapSettings>> GetExportedMapsAsync();

        Task<bool> TryClearRecentAsync();

        Task<bool> TryAddMapsToCollectionAsync(IList<Beatmap> beatmaps, Collection collection);

        Task<bool> TryRemoveLocalAllAsync();

        Task<bool> TryAddNewMapsAsync(IEnumerable<Beatmap> beatmaps);

        Task SyncMapsFromOsuDbAsync(IEnumerable<Beatmap> beatmaps, bool addOnly);

        Task<(bool found, string thumbPath)> TryGetMapThumbAsync(Guid beatmapDbId);

        Task<bool> TrySetMapThumbAsync(Guid beatmapDbId, string thumbPath);
    }
}