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

        public async Task<Beatmap> GetBeatmapByIdentifiableAsync(IMapIdentifiable beatmap)
        {
            try
            {
                var map = await _inner.GetBeatmapByIdentifiableAsync(beatmap);
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

        public Task<bool> TryRemoveFromRecentAsync(MapIdentity identity)
            => RunAsync(() => _inner.TryRemoveFromRecentAsync(identity), "Error while removing beatmap from recent",
                false);

        public Task<BeatmapSettings> GetMapFromDbAsync(IMapIdentifiable beatmap)
            => RunAsync(() => _inner.GetMapFromDbAsync(beatmap), "Error while getting beatmap settings from database",
                null);

        public Task<bool> TryRemoveMapFromCollectionAsync(IMapIdentifiable identity, Collection collection)
            => RunAsync(() => _inner.TryRemoveMapFromCollectionAsync(identity, collection),
                "Error while removing beatmap from collection", false);

        public Task<PaginationQueryResult<Beatmap>> SearchBeatmapPageAsync(string searchText, BeatmapSortMode sortMode,
            int startIndex, int count)
            => RunAsync(() => _inner.SearchBeatmapPageAsync(searchText, sortMode, startIndex, count),
                "Error while searching for beatmaps by page", new PaginationQueryResult<Beatmap>([], 0));

        public Task<List<Beatmap>> SearchBeatmapByOptionsAsync(string searchText, BeatmapSortMode sortMode,
            int startIndex,
            int count)
            => RunAsync(() => _inner.SearchBeatmapByOptionsAsync(searchText, sortMode, startIndex, count),
                "Error while searching for beatmaps", []);

        public Task<List<Beatmap>> GetBeatmapsFromFolderAsync(string folderName)
            => RunAsync(() => _inner.GetBeatmapsFromFolderAsync(folderName), "Error while getting beatmaps from folder",
                []);

        public Task<List<Collection>> GetCollectionsAsync()
            => RunAsync(() => _inner.GetCollectionsAsync(), "Error while getting collections", []);

        public Task<List<Collection>> GetCollectionsByMapAsync(BeatmapSettings beatmapSettings)
            => RunAsync(() => _inner.GetCollectionsByMapAsync(beatmapSettings),
                "Error while getting collections by map",
                []);

        public Task<bool> TryAddCollectionAsync(string collectionName)
            => RunAsync(() => _inner.TryAddCollectionAsync(collectionName),
                $"Error while adding collection \"{collectionName}\"",
                false);

        public Task<List<Beatmap>> GetBeatmapsByIdentifiableAsync(IEnumerable<IMapIdentifiable> mapIdentities)
            => RunAsync(() => _inner.GetBeatmapsByIdentifiableAsync(mapIdentities),
                "Error while getting beatmaps by IMapIdentifiable from database", []);

        public Task<bool> TryUpdateCollectionAsync(Collection collection)
            => RunAsync(() => _inner.TryUpdateCollectionAsync(collection),
                $"Error while updating collection \"{collection?.Name}\"", false);

        public Task<bool> TryUpdateMapAsync(IMapIdentifiable beatmap, int? offset = null)
            => RunAsync(() => _inner.TryUpdateMapAsync(beatmap, offset),
                $"Error while updating map offset \"{beatmap?.GetIdentity()}\"", false);

        public async Task<Collection> GetCollectionByIdAsync(string id)
        {
            try
            {
                var collection = await _inner.GetCollectionByIdAsync(id);
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

        public Task<List<BeatmapSettings>> GetMapsFromCollectionAsync(Collection collection)
            => RunAsync(() => _inner.GetMapsFromCollectionAsync(collection), "Error while getting maps from collection",
                []);

        public Task<List<Beatmap>> GetBeatmapsByMapInfoAsync(List<BeatmapSettings> settings, TimeSortMode sortMode)
            => RunAsync(() => _inner.GetBeatmapsByMapInfoAsync(settings, sortMode),
                "Error while getting maps by settings",
                []);

        public Task<bool> TryRemoveCollectionAsync(Collection collection)
            => RunAsync(() => _inner.TryRemoveCollectionAsync(collection), "Error while removing collection", false);

        public Task<bool> TryAddMapExportAsync(IMapIdentifiable mapIdentity, string path)
            => RunAsync(() => _inner.TryAddMapExportAsync(mapIdentity, path), "Error while updating exported map",
                false);

        public Task<List<BeatmapSettings>> GetRecentListAsync()
            => RunAsync(() => _inner.GetRecentListAsync(), "Error while getting recent list", []);

        public Task<List<BeatmapSettings>> GetExportedMapsAsync()
            => RunAsync(() => _inner.GetExportedMapsAsync(), "Error while getting exported list", []);

        public Task<bool> TryClearRecentAsync()
            => RunAsync(() => _inner.TryClearRecentAsync(), "Error while clearing recent", false);

        public Task<bool> TryAddMapsToCollectionAsync(IList<Beatmap> beatmaps, Collection collection)
            => RunAsync(() => _inner.TryAddMapsToCollectionAsync(beatmaps, collection),
                "Error while adding maps to collection",
                false);

        public Task<bool> TryRemoveLocalAllAsync()
            => RunAsync(() => _inner.TryRemoveLocalAllAsync(), "Error while removing local beatmaps", false);

        public Task<bool> TryAddNewMapsAsync(IEnumerable<Beatmap> beatmaps)
            => RunAsync(() => _inner.TryAddNewMapsAsync(beatmaps), "Error while adding new beatmaps", false);

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

        public async Task<(bool found, string thumbPath)> TryGetMapThumbAsync(Guid beatmapDbId)
        {
            try
            {
                return await _inner.TryGetMapThumbAsync(beatmapDbId);
            }
            catch (Exception ex)
            {
                NotifyError(ex, "Error while getting map thumbnail");
                return (false, null);
            }
        }

        public Task<bool> TrySetMapThumbAsync(Guid beatmapDbId, string thumbPath)
            => RunAsync(() => _inner.TrySetMapThumbAsync(beatmapDbId, thumbPath), "Error while setting map thumbnail",
                false);

        private async Task<T> RunAsync<T>(Func<Task<T>> action, string message, T fallback)
        {
            try
            {
                return await action();
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