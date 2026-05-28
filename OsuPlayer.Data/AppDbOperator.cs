using System;
using System.Collections.Generic;
using System.Data.Common;
using Coosu.Beatmap.MetaData;
using Microsoft.Data.Sqlite;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.Data
{
    public class AppDbOperator
    {
        public const string TABLE_BEATMAP = "beatmap";

        private readonly Func<OsuPlayerDbContext> _dbContextFactory;

        public AppDbOperator()
            : this(() => new OsuPlayerDbContext())
        {
        }

        public AppDbOperator(Func<OsuPlayerDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public static void ValidateDb()
        {
            OsuPlayerDbContext.ValidateDb();
        }

        [Obsolete("Dapper queries should use OsuPlayerDbContext.Database.GetDbConnection() so they share the DbContext lifetime.")]
        public DbConnection GetDapperConnection()
        {
            return new SqliteConnection(OsuPlayerDbContext.DefaultConnectionString);
        }

        public BeatmapSettings GetMapFromDb(IMapIdentifiable id)
        {
            using var db = _dbContextFactory();
            return db.GetMapFromDb(id);
        }

        public List<BeatmapSettings> GetRecentList()
        {
            using var db = _dbContextFactory();
            return db.GetRecentList();
        }

        public List<BeatmapSettings> GetExportedMaps()
        {
            using var db = _dbContextFactory();
            return db.GetExportedMaps();
        }

        public List<BeatmapSettings> GetMapsFromCollection(Collection collection)
        {
            using var db = _dbContextFactory();
            return db.GetMapsFromCollection(collection);
        }

        public List<Collection> GetCollections()
        {
            using var db = _dbContextFactory();
            return db.GetCollections();
        }

        public List<Collection> GetCollectionsByMap(BeatmapSettings beatmapSettings)
        {
            using var db = _dbContextFactory();
            return db.GetCollectionsByMap(beatmapSettings);
        }

        public void AddCollection(string name, bool locked = false)
        {
            using var db = _dbContextFactory();
            db.AddCollection(name, locked);
        }

        public Collection GetCollectionById(string id)
        {
            using var db = _dbContextFactory();
            return db.GetCollectionById(id);
        }

        public void AddMapsToCollection(IList<Beatmap> beatmaps, Collection collection)
        {
            using var db = _dbContextFactory();
            db.AddMapsToCollection(beatmaps, collection);
        }

        public void UpdateCollection(Collection collection)
        {
            using var db = _dbContextFactory();
            db.UpdateCollection(collection);
        }

        public void UpdateMap(IMapIdentifiable id, int? offset = null)
        {
            using var db = _dbContextFactory();
            db.UpdateMap(id, offset);
        }

        public void AddMapExport(IMapIdentifiable id, string exportFilePath)
        {
            using var db = _dbContextFactory();
            db.AddMapExport(id, exportFilePath);
        }

        public void RemoveMapExport(IMapIdentifiable id)
        {
            using var db = _dbContextFactory();
            db.RemoveMapExport(id);
        }

        public void RemoveFromRecent(IMapIdentifiable id)
        {
            using var db = _dbContextFactory();
            db.RemoveFromRecent(id);
        }

        public void ClearRecent()
        {
            using var db = _dbContextFactory();
            db.ClearRecent();
        }

        public void RemoveCollection(Collection collection)
        {
            using var db = _dbContextFactory();
            db.RemoveCollection(collection);
        }

        public void RemoveMapFromCollection(IMapIdentifiable id, Collection collection)
        {
            using var db = _dbContextFactory();
            db.RemoveMapFromCollection(id, collection);
        }

        public bool GetMapThumb(Guid beatmapDbId, out string thumbPath)
        {
            using var db = _dbContextFactory();
            return db.GetMapThumb(beatmapDbId, out thumbPath);
        }

        public bool GetMapThumb(Beatmap beatmap, out string thumbPath)
        {
            using var db = _dbContextFactory();
            return db.GetMapThumb(beatmap, out thumbPath);
        }

        public void SetMapThumb(Guid beatmapDbId, string thumbPath)
        {
            using var db = _dbContextFactory();
            db.SetMapThumb(beatmapDbId, thumbPath);
        }

        public void SetMapThumb(Beatmap beatmap, string thumbPath)
        {
            using var db = _dbContextFactory();
            db.SetMapThumb(beatmap, thumbPath);
        }

        public void SetMapSbInfo(Guid beatmapDbId, StoryboardInfo sbInfo)
        {
            using var db = _dbContextFactory();
            db.SetMapSbInfo(beatmapDbId, sbInfo);
        }

        public void SetMapSbInfo(Beatmap beatmap, StoryboardInfo sbInfo)
        {
            using var db = _dbContextFactory();
            db.SetMapSbInfo(beatmap, sbInfo);
        }
    }
}
