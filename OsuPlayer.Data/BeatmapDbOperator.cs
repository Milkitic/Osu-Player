using System.Collections.Generic;
using System.Threading.Tasks;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared.Models;

namespace Milky.OsuPlayer.Data
{
    public static class BeatmapDbOperator
    {
        public static List<Beatmap> SearchBeatmapByOptions(this AppDbOperator op, string searchText, BeatmapSortMode beatmapSortMode, int startIndex, int count)
        {
            using var db = new OsuPlayerDbContext();
            return db.SearchBeatmapByOptions(searchText, beatmapSortMode, startIndex, count);
        }

        public static List<Beatmap> GetAllBeatmaps(this AppDbOperator op)
        {
            using var db = new OsuPlayerDbContext();
            return db.GetAllBeatmaps();
        }

        public static Beatmap GetBeatmapByIdentifiable(this AppDbOperator op, IMapIdentifiable id)
        {
            using var db = new OsuPlayerDbContext();
            return db.GetBeatmapByIdentifiable(id);
        }

        public static List<Beatmap> GetBeatmapsByMapInfo(this AppDbOperator op, List<BeatmapSettings> reqList, TimeSortMode sortMode)
        {
            using var db = new OsuPlayerDbContext();
            return db.GetBeatmapsByMapInfo(reqList, sortMode);
        }

        public static List<Beatmap> GetBeatmapsFromFolder(this AppDbOperator op, string folder)
        {
            using var db = new OsuPlayerDbContext();
            return db.GetBeatmapsFromFolder(folder);
        }

        public static List<Beatmap> GetBeatmapsByIdentifiable<T>(this AppDbOperator op, IEnumerable<T> reqList)
            where T : IMapIdentifiable
        {
            using var db = new OsuPlayerDbContext();
            return db.GetBeatmapsByIdentifiable(reqList);
        }

        public static async Task SyncMapsFromOsuDbAsync(this AppDbOperator op, IEnumerable<Beatmap> newList, bool addOnly)
        {
            await Task.Run(() =>
            {
                using var db = new OsuPlayerDbContext();
                return db.SyncMapsFromOsuDbAsync(newList, addOnly);
            }).ConfigureAwait(false);
        }

        public static void AddNewMaps(this AppDbOperator op, IEnumerable<Beatmap> beatmaps)
        {
            using var db = new OsuPlayerDbContext();
            db.AddNewMaps(beatmaps);
        }

        public static void AddNewMaps(this AppDbOperator op, params Beatmap[] beatmaps)
        {
            using var db = new OsuPlayerDbContext();
            db.AddNewMaps(beatmaps);
        }

        public static void RemoveLocalAll(this AppDbOperator op)
        {
            using var db = new OsuPlayerDbContext();
            db.RemoveLocalAll();
        }

        public static void RemoveSyncedAll(this AppDbOperator op)
        {
            using var db = new OsuPlayerDbContext();
            db.RemoveSyncedAll();
        }
    }

    public enum TimeSortMode
    {
        PlayTime, AddTime
    }
}
