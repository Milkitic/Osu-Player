using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared.Models;
using Milky.OsuPlayer.Utils;

namespace Milky.OsuPlayer.Services
{
    public sealed class NotifyingPlayerDataService : IPlayerDataService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IPlayerDataStore _inner;
        private readonly IAppNotificationService _notifications;

        public NotifyingPlayerDataService(IPlayerDataStore inner, IAppNotificationService notifications)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        }

        public Beatmap GetBeatmapByIdentifiable(IMapIdentifiable beatmap)
        {
            try
            {
                var map = _inner.GetBeatmapByIdentifiable(beatmap);
                if (map is null)
                {
                    _notifications.Push(I18NUtil.GetString("err-mapNotInDb"), I18NUtil.GetString("text-error"));
                }

                return map;
            }
            catch (Exception ex)
            {
                NotifyError(ex, "Error while getting beatmap by IMapIdentifiable from database");
                return null;
            }
        }

        public bool TryRemoveFromRecent(MapIdentity identity)
            => Run(() => _inner.TryRemoveFromRecent(identity), "Error while removing beatmap from recent", false);

        public BeatmapSettings GetMapFromDb(IMapIdentifiable beatmap)
            => Run(() => _inner.GetMapFromDb(beatmap), "Error while getting beatmap settings from database", null);

        public bool TryRemoveMapFromCollection(IMapIdentifiable identity, Collection collection)
            => Run(() => _inner.TryRemoveMapFromCollection(identity, collection),
                "Error while removing beatmap from collection", false);

        public List<Beatmap> SearchBeatmapByOptions(string searchText, BeatmapSortMode sortMode, int startIndex,
            int count)
            => Run(() => _inner.SearchBeatmapByOptions(searchText, sortMode, startIndex, count),
                "Error while searching for beatmaps", []);

        public List<Beatmap> GetBeatmapsFromFolder(string folderName)
            => Run(() => _inner.GetBeatmapsFromFolder(folderName), "Error while getting beatmaps from folder",
                []);

        public List<Collection> GetCollections()
            => Run(() => _inner.GetCollections(), "Error while getting collections", []);

        public List<Collection> GetCollectionsByMap(BeatmapSettings beatmapSettings)
            => Run(() => _inner.GetCollectionsByMap(beatmapSettings), "Error while getting collections by map",
                []);

        public bool TryAddCollection(string collectionName)
            => Run(() => _inner.TryAddCollection(collectionName), $"Error while adding collection \"{collectionName}\"",
                false);

        public List<Beatmap> GetBeatmapsByIdentifiable(IEnumerable<IMapIdentifiable> mapIdentities)
            => Run(() => _inner.GetBeatmapsByIdentifiable(mapIdentities),
                "Error while getting beatmaps by IMapIdentifiable from database", []);

        public bool TryUpdateCollection(Collection collection)
            => Run(() => _inner.TryUpdateCollection(collection),
                $"Error while updating collection \"{collection?.Name}\"", false);

        public bool TryUpdateMap(IMapIdentifiable beatmap, int? offset = null)
            => Run(() => _inner.TryUpdateMap(beatmap, offset),
                $"Error while updating map offset \"{beatmap?.GetIdentity()}\"", false);

        public Collection GetCollectionById(string id)
        {
            try
            {
                var collection = _inner.GetCollectionById(id);
                if (collection is null)
                {
                    _notifications.Push(I18NUtil.GetString("err-collectionNotInDb"), I18NUtil.GetString("text-error"));
                }

                return collection;
            }
            catch (Exception ex)
            {
                NotifyError(ex, "Error while getting collection by id");
                return null;
            }
        }

        public List<BeatmapSettings> GetMapsFromCollection(Collection collection)
            => Run(() => _inner.GetMapsFromCollection(collection), "Error while getting maps from collection",
                []);

        public List<Beatmap> GetBeatmapsByMapInfo(List<BeatmapSettings> settings, TimeSortMode sortMode)
            => Run(() => _inner.GetBeatmapsByMapInfo(settings, sortMode), "Error while getting maps by settings",
                []);

        public bool TryRemoveCollection(Collection collection)
            => Run(() => _inner.TryRemoveCollection(collection), "Error while removing collection", false);

        public bool TryAddMapExport(IMapIdentifiable mapIdentity, string path)
            => Run(() => _inner.TryAddMapExport(mapIdentity, path), "Error while updating exported map", false);

        public List<BeatmapSettings> GetRecentList()
            => Run(() => _inner.GetRecentList(), "Error while getting recent list", []);

        public List<BeatmapSettings> GetExportedMaps()
            => Run(() => _inner.GetExportedMaps(), "Error while getting exported list", []);

        public bool TryClearRecent()
            => Run(() => _inner.TryClearRecent(), "Error while clearing recent", false);

        public bool TryAddMapsToCollection(IList<Beatmap> beatmaps, Collection collection)
            => Run(() => _inner.TryAddMapsToCollection(beatmaps, collection), "Error while adding maps to collection",
                false);

        public bool TryRemoveLocalAll()
            => Run(() => _inner.TryRemoveLocalAll(), "Error while removing local beatmaps", false);

        public bool TryAddNewMaps(IEnumerable<Beatmap> beatmaps)
            => Run(() => _inner.TryAddNewMaps(beatmaps), "Error while adding new beatmaps", false);

        public async Task SyncMapsFromOsuDbAsync(IEnumerable<Beatmap> beatmaps, bool addOnly)
        {
            try
            {
                await _inner.SyncMapsFromOsuDbAsync(beatmaps, addOnly);
            }
            catch (Exception ex)
            {
                NotifyError(ex, "Error while syncing osu!db maps");
            }
        }

        public bool TryGetMapThumb(Guid beatmapDbId, out string thumbPath)
        {
            try
            {
                return _inner.TryGetMapThumb(beatmapDbId, out thumbPath);
            }
            catch (Exception ex)
            {
                NotifyError(ex, "Error while getting map thumbnail");
                thumbPath = null;
                return false;
            }
        }

        public bool TrySetMapThumb(Guid beatmapDbId, string thumbPath)
            => Run(() => _inner.TrySetMapThumb(beatmapDbId, thumbPath), "Error while setting map thumbnail", false);

        private T Run<T>(Func<T> action, string message, T fallback)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                NotifyError(ex, message);
                return fallback;
            }
        }

        private void NotifyError(Exception ex, string message)
        {
            Logger.Error(ex, message);
            _notifications.Push($"{message}: {ex.Message}");
        }
    }
}
