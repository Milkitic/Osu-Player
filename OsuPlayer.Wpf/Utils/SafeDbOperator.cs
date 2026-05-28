using System;
using System.Collections.Generic;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation.Annotations;
using Milky.OsuPlayer.Shared.Models;
using Milky.OsuPlayer.UiComponents.NotificationComponent;

namespace Milky.OsuPlayer.Utils
{
    public class SafeDbOperator
    {
        [CanBeNull]
        public Beatmap GetBeatmapByIdentifiable(IMapIdentifiable beatmap)
        {
            Beatmap map;
            try
            {
                using var db = new OsuPlayerDbContext();
                map = db.GetBeatmapByIdentifiable(beatmap);
                if (map is null)
                {
                    Notification.Push(I18NUtil.GetString("err-mapNotInDb"), I18NUtil.GetString("text-error"));
                }
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while getting beatmap by IMapIdentifiable from database: {ex.Message}");
                map = null;
            }

            return map;
        }

        public bool TryRemoveFromRecent(MapIdentity identity)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                db.RemoveFromRecent(identity);
                return true;
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while removing beatmap from recent: {ex.Message}");
                return false;
            }
        }

        public bool TryRemoveMapFromCollection(IMapIdentifiable identity, Collection collection)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                db.RemoveMapFromCollection(identity, collection);
                return true;
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while removing beatmap from recent: {ex.Message}");
                return false;
            }
        }

        public List<Beatmap> SearchBeatmapByOptions(string searchText, BeatmapSortMode sortMode, int startIndex,
            int count)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                return db.SearchBeatmapByOptions(searchText, sortMode, startIndex, count);
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while searching for beatmaps: {ex.Message}");
                return new List<Beatmap>();
            }
        }

        public List<Beatmap> GetBeatmapsFromFolder(string folderName)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                return db.GetBeatmapsFromFolder(folderName);
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while getting beatmaps from folder: {ex.Message}");
                return new List<Beatmap>();
            }
        }

        public List<Collection> GetCollections()
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                return db.GetCollections();
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while getting collections: {ex.Message}");
                return new List<Collection>();
            }
        }

        public bool TryAddCollection(string collectionName)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                db.AddCollection(collectionName);
                return true;
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while adding collection \"{collectionName}\": {ex.Message}");
                return false;
            }
        }

        public List<Beatmap> GetBeatmapsByIdentifiable(IEnumerable<IMapIdentifiable> mapIdentities)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                return db.GetBeatmapsByIdentifiable(mapIdentities);
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while getting beatmaps by IMapIdentifiable from database: {ex.Message}");
                return new List<Beatmap>();
            }
        }

        public bool TryUpdateCollection(Collection collection)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                db.UpdateCollection(collection);
                return true;
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while updating collection \"{collection?.Name}\": {ex.Message}");
                return false;
            }
        }

        public bool TryUpdateMap(Beatmap beatmap, int? offset = null)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                db.UpdateMap(beatmap, offset);
                return true;
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while updating map offset \"{beatmap?.GetIdentity()}\": {ex.Message}");
                return false;
            }
        }

        [CanBeNull]
        public Collection GetCollectionById(string id)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                var collection = db.GetCollectionById(id);
                if (collection is null)
                {
                    Notification.Push(I18NUtil.GetString("err-collectionNotInDb"), I18NUtil.GetString("text-error"));
                }

                return collection;
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while getting collection by id: {ex.Message}");
                return null;
            }
        }

        public List<BeatmapSettings> GetMapsFromCollection(Collection collection)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                return db.GetMapsFromCollection(collection);
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while getting maps from collection: {ex.Message}");
                return new List<BeatmapSettings>();
            }
        }

        public List<Beatmap> GetBeatmapsByMapInfo(List<BeatmapSettings> settings, TimeSortMode sortMode)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                return db.GetBeatmapsByMapInfo(settings, sortMode);
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while getting maps from collection: {ex.Message}");
                return new List<Beatmap>();
            }
        }

        public bool TryRemoveCollection(Collection collection)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                db.RemoveCollection(collection);
                return true;
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while removing collection: {ex.Message}");
                return false;
            }
        }

        public bool TryAddMapExport(MapIdentity mapIdentity, string path)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                db.AddMapExport(mapIdentity, path);
                return true;
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while removing collection: {ex.Message}");
                return false;
            }
        }

        public List<BeatmapSettings> GetRecentList()
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                return db.GetRecentList();
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while getting recent list: {ex.Message}");
                return new List<BeatmapSettings>();
            }
        }

        public List<BeatmapSettings> GetExportedMaps()
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                return db.GetExportedMaps();
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while getting exported list: {ex.Message}");
                return new List<BeatmapSettings>();
            }
        }

        public bool TryClearRecent()
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                db.ClearRecent();
                return true;
            }
            catch (Exception ex)
            {
                Notification.Push($"Error while clearing recent: {ex.Message}");
                return false;
            }
        }
    }
}