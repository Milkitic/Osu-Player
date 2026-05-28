using System;
using System.Collections.Generic;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared.Models;

namespace Milky.OsuPlayer.Services
{
    public sealed class PlayerDataService : IPlayerDataService
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

        public Beatmap GetBeatmapByIdentifiable(IMapIdentifiable beatmap)
        {
            using var db = _createDbContext();
            return db.GetBeatmapByIdentifiable(beatmap);
        }

        public bool TryRemoveFromRecent(MapIdentity identity)
        {
            using var db = _createDbContext();
            db.RemoveFromRecent(identity);
            return true;
        }

        public bool TryRemoveMapFromCollection(IMapIdentifiable identity, Collection collection)
        {
            using var db = _createDbContext();
            db.RemoveMapFromCollection(identity, collection);
            return true;
        }

        public List<Beatmap> SearchBeatmapByOptions(string searchText, BeatmapSortMode sortMode, int startIndex, int count)
        {
            using var db = _createDbContext();
            return db.SearchBeatmapByOptions(searchText, sortMode, startIndex, count);
        }

        public List<Beatmap> GetBeatmapsFromFolder(string folderName)
        {
            using var db = _createDbContext();
            return db.GetBeatmapsFromFolder(folderName);
        }

        public List<Collection> GetCollections()
        {
            using var db = _createDbContext();
            return db.GetCollections();
        }

        public bool TryAddCollection(string collectionName)
        {
            using var db = _createDbContext();
            db.AddCollection(collectionName);
            return true;
        }

        public List<Beatmap> GetBeatmapsByIdentifiable(IEnumerable<IMapIdentifiable> mapIdentities)
        {
            using var db = _createDbContext();
            return db.GetBeatmapsByIdentifiable(mapIdentities);
        }

        public bool TryUpdateCollection(Collection collection)
        {
            using var db = _createDbContext();
            db.UpdateCollection(collection);
            return true;
        }

        public bool TryUpdateMap(IMapIdentifiable beatmap, int? offset = null)
        {
            using var db = _createDbContext();
            db.UpdateMap(beatmap, offset);
            return true;
        }

        public Collection GetCollectionById(string id)
        {
            using var db = _createDbContext();
            return db.GetCollectionById(id);
        }

        public List<BeatmapSettings> GetMapsFromCollection(Collection collection)
        {
            using var db = _createDbContext();
            return db.GetMapsFromCollection(collection);
        }

        public List<Beatmap> GetBeatmapsByMapInfo(List<BeatmapSettings> settings, TimeSortMode sortMode)
        {
            using var db = _createDbContext();
            return db.GetBeatmapsByMapInfo(settings, sortMode);
        }

        public bool TryRemoveCollection(Collection collection)
        {
            using var db = _createDbContext();
            db.RemoveCollection(collection);
            return true;
        }

        public bool TryAddMapExport(IMapIdentifiable mapIdentity, string path)
        {
            using var db = _createDbContext();
            db.AddMapExport(mapIdentity, path);
            return true;
        }

        public List<BeatmapSettings> GetRecentList()
        {
            using var db = _createDbContext();
            return db.GetRecentList();
        }

        public List<BeatmapSettings> GetExportedMaps()
        {
            using var db = _createDbContext();
            return db.GetExportedMaps();
        }

        public bool TryClearRecent()
        {
            using var db = _createDbContext();
            db.ClearRecent();
            return true;
        }

        public bool TryAddMapsToCollection(IList<Beatmap> beatmaps, Collection collection)
        {
            using var db = _createDbContext();
            db.AddMapsToCollection(beatmaps, collection);
            return true;
        }
    }
}