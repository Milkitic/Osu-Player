using System.Collections.Generic;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared.Models;

namespace Milky.OsuPlayer.Services
{
    public interface IPlayerDataService
    {
        Beatmap GetBeatmapByIdentifiable(IMapIdentifiable beatmap);

        bool TryRemoveFromRecent(MapIdentity identity);

        bool TryRemoveMapFromCollection(IMapIdentifiable identity, Collection collection);

        List<Beatmap> SearchBeatmapByOptions(string searchText, BeatmapSortMode sortMode, int startIndex, int count);

        List<Beatmap> GetBeatmapsFromFolder(string folderName);

        List<Collection> GetCollections();

        bool TryAddCollection(string collectionName);

        List<Beatmap> GetBeatmapsByIdentifiable(IEnumerable<IMapIdentifiable> mapIdentities);

        bool TryUpdateCollection(Collection collection);

        bool TryUpdateMap(IMapIdentifiable beatmap, int? offset = null);

        Collection GetCollectionById(string id);

        List<BeatmapSettings> GetMapsFromCollection(Collection collection);

        List<Beatmap> GetBeatmapsByMapInfo(List<BeatmapSettings> settings, TimeSortMode sortMode);

        bool TryRemoveCollection(Collection collection);

        bool TryAddMapExport(IMapIdentifiable mapIdentity, string path);

        List<BeatmapSettings> GetRecentList();

        List<BeatmapSettings> GetExportedMaps();

        bool TryClearRecent();

        bool TryAddMapsToCollection(IList<Beatmap> beatmaps, Collection collection);
    }
}