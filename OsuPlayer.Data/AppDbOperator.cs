using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using Coosu.Beatmap.MetaData;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.Data
{
    public class AppDbOperator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public const string TABLE_BEATMAP = "beatmap";

        static AppDbOperator()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            SqlMapper.AddTypeHandler(new GuidTypeHandler());
        }

        private static readonly ThreadLocal<DbConnection> DapperConnection = new ThreadLocal<DbConnection>(() =>
            new SqliteConnection(OsuPlayerDbContext.DefaultConnectionString));

        public DbConnection GetDapperConnection()
        {
            return DapperConnection.Value;
        }

        private sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
        {
            public override void SetValue(IDbDataParameter parameter, Guid value)
            {
                parameter.Value = value.ToString();
            }

            public override Guid Parse(object value)
            {
                return value switch
                {
                    Guid guid => guid,
                    string text => Guid.Parse(text),
                    byte[] bytes => new Guid(bytes),
                    _ => Guid.Parse(value.ToString())
                };
            }
        }

        private static OsuPlayerDbContext CreateDbContext()
        {
            return new OsuPlayerDbContext();
        }

        public static void ValidateDb()
        {
            try
            {
                using var db = CreateDbContext();
                db.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while validating local database.");
                throw;
            }
        }

        public BeatmapSettings GetMapFromDb(IMapIdentifiable id)
        {
            try
            {
                if (id.IsMapTemporary())
                {
                    Logger.Debug("需确认加入自定义目录后才可继续");
                }

                using var db = CreateDbContext();
                var map = db.BeatmapSettings.FirstOrDefault(k =>
                    k.Version == id.Version &&
                    k.FolderName == id.FolderName &&
                    k.InOwnDb == id.InOwnDb);

                if (map != null)
                {
                    return map;
                }

                map = new BeatmapSettings
                {
                    Id = Guid.NewGuid().ToString(),
                    Version = id.Version,
                    FolderName = id.FolderName,
                    InOwnDb = id.InOwnDb,
                    Offset = 0
                };

                db.BeatmapSettings.Add(map);
                db.SaveChanges();
                return map;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetMapFromDb().");
                throw;
            }
        }

        public List<BeatmapSettings> GetRecentList()
        {
            using var db = CreateDbContext();
            return db.BeatmapSettings
                .Where(k => k.LastPlayTime != null)
                .OrderBy(k => k.LastPlayTime)
                .ToList();
        }

        public List<BeatmapSettings> GetExportedMaps()
        {
            using var db = CreateDbContext();
            return db.BeatmapSettings
                .Where(k => k.ExportFile != null && k.ExportFile.Trim() != string.Empty)
                .ToList();
        }

        public List<BeatmapSettings> GetMapsFromCollection(Collection collection)
        {
            using var db = CreateDbContext();
            return db.CollectionRelations
                .Where(relation => relation.CollectionId == collection.Id)
                .Join(db.BeatmapSettings,
                    relation => relation.MapId,
                    map => map.Id,
                    (relation, map) => new BeatmapSettings(
                        map.Id,
                        map.Version,
                        map.FolderName,
                        map.Offset,
                        map.LastPlayTime,
                        map.ExportFile,
                        relation.AddTime)
                    {
                        InOwnDb = map.InOwnDb
                    })
                .ToList();
        }

        public List<Collection> GetCollections()
        {
            using var db = CreateDbContext();
            return db.Collections.ToList();
        }

        public List<Collection> GetCollectionsByMap(BeatmapSettings beatmapSettings)
        {
            if (beatmapSettings.IsMapTemporary())
            {
                Logger.Debug("需确认加入自定义目录后才可继续");
            }

            using var db = CreateDbContext();
            return db.CollectionRelations
                .Where(relation => relation.MapId == beatmapSettings.Id)
                .Join(db.Collections,
                    relation => relation.CollectionId,
                    collection => collection.Id,
                    (_, collection) => collection)
                .ToList();
        }

        public void AddCollection(string name, bool locked = false)
        {
            using var db = CreateDbContext();
            db.Collections.Add(new Collection
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Locked = locked ? 1 : 0,
                Index = 0,
                CreateTime = DateTime.Now
            });
            db.SaveChanges();
        }

        private void AddCollection(Collection collection)
        {
            using var db = CreateDbContext();
            db.Collections.Add(new Collection
            {
                Id = string.IsNullOrWhiteSpace(collection.Id) ? Guid.NewGuid().ToString() : collection.Id,
                Name = collection.Name,
                Locked = collection.LockedBool ? 1 : 0,
                Index = collection.Index,
                ImagePath = collection.ImagePath,
                Description = collection.Description,
                CreateTime = collection.CreateTime == default ? DateTime.Now : collection.CreateTime
            });
            db.SaveChanges();
        }

        public Collection GetCollectionById(string id)
        {
            using var db = CreateDbContext();
            return db.Collections.FirstOrDefault(k => k.Id == id);
        }

        public void AddMapsToCollection(IList<Beatmap> beatmaps, Collection collection)
        {
            if (beatmaps.Count < 1) return;

            var relations = new List<CollectionRelation>(beatmaps.Count);
            var addTime = DateTime.Now;
            foreach (var beatmap in beatmaps)
            {
                if (beatmap.IsMapTemporary())
                {
                    Logger.Debug("需确认加入自定义目录后才可继续");
                }

                var map = GetMapFromDb(beatmap.GetIdentity());
                relations.Add(new CollectionRelation
                {
                    Id = Guid.NewGuid().ToString(),
                    CollectionId = collection.Id,
                    MapId = map.Id,
                    AddTime = addTime
                });
            }

            using var db = CreateDbContext();
            db.CollectionRelations.AddRange(relations);
            db.SaveChanges();
        }

        public void UpdateCollection(Collection collection)
        {
            using var db = CreateDbContext();
            var result = db.Collections.FirstOrDefault(k => k.Id == collection.Id);
            if (result == null)
            {
                AddCollection(collection);
                return;
            }

            result.Name = collection.Name;
            result.Locked = collection.LockedBool ? 1 : 0;
            result.Index = collection.Index;
            result.ImagePath = collection.ImagePath;
            result.Description = collection.Description;
            result.CreateTime = collection.CreateTime;
            db.SaveChanges();
        }

        public void UpdateMap(IMapIdentifiable id, int? offset = null)
        {
            var updateColumns = new Action<BeatmapSettings>(map => map.LastPlayTime = DateTime.Now);
            if (offset != null)
            {
                updateColumns += map => map.Offset = offset.Value;
            }

            InnerUpdateMap(id, updateColumns);
        }

        public void AddMapExport(IMapIdentifiable id, string exportFilePath)
        {
            InnerUpdateMap(id, map => map.ExportFile = exportFilePath);
        }

        public void RemoveMapExport(IMapIdentifiable id)
        {
            InnerUpdateMap(id, map => map.ExportFile = null);
        }

        public void RemoveFromRecent(IMapIdentifiable id)
        {
            InnerUpdateMap(id, map => map.LastPlayTime = null);
        }

        public void ClearRecent()
        {
            using var db = CreateDbContext();
            db.BeatmapSettings.ExecuteUpdate(setters => setters.SetProperty(map => map.LastPlayTime, (DateTime?)null));
        }

        public void RemoveCollection(Collection collection)
        {
            using var db = CreateDbContext();
            using var transaction = db.Database.BeginTransaction();
            db.Collections.Where(k => k.Id == collection.Id).ExecuteDelete();
            db.CollectionRelations.Where(k => k.CollectionId == collection.Id).ExecuteDelete();
            transaction.Commit();
        }

        public void RemoveMapFromCollection(IMapIdentifiable id, Collection collection)
        {
            if (id.IsMapTemporary())
            {
                Logger.Debug("需确认加入自定义目录后才可继续");
            }

            var map = GetMapFromDb(id);
            using var db = CreateDbContext();
            db.CollectionRelations
                .Where(k => k.CollectionId == collection.Id && k.MapId == map.Id)
                .ExecuteDelete();
        }

        public bool GetMapThumb(Guid beatmapDbId, out string thumbPath)
        {
            using var db = CreateDbContext();
            var thumb = db.MapThumbs.FirstOrDefault(k => k.MapId == beatmapDbId);
            thumbPath = thumb?.ThumbPath;
            return thumb != null;
        }

        public bool GetMapThumb(Beatmap beatmap, out string thumbPath)
        {
            if (beatmap.IsMapTemporary())
            {
                Logger.Debug("需确认加入自定义目录后才可继续");
            }

            return GetMapThumb(beatmap.Id, out thumbPath);
        }

        public void SetMapThumb(Guid beatmapDbId, string thumbPath)
        {
            using var db = CreateDbContext();
            var thumb = db.MapThumbs.FirstOrDefault(k => k.MapId == beatmapDbId);
            if (thumb == null)
            {
                db.MapThumbs.Add(new MapThumb
                {
                    Id = Guid.NewGuid().ToString(),
                    MapId = beatmapDbId,
                    ThumbPath = thumbPath
                });
            }
            else
            {
                thumb.ThumbPath = thumbPath;
            }

            db.SaveChanges();
        }

        public void SetMapThumb(Beatmap beatmap, string thumbPath)
        {
            SetMapThumb(beatmap.Id, thumbPath);
        }

        public void SetMapSbInfo(Guid beatmapDbId, StoryboardInfo sbInfo)
        {
            if (sbInfo.IsMapTemporary())
            {
                Logger.Debug("需确认加入自定义目录后才可继续");
            }

            var mapId = beatmapDbId.ToString();
            using var db = CreateDbContext();
            var result = db.StoryboardInfos.FirstOrDefault(k => k.MapId == mapId);
            if (result == null)
            {
                db.StoryboardInfos.Add(new StoryboardInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    MapId = mapId,
                    SbThumbPath = sbInfo.SbThumbPath,
                    SbThumbVideoPath = sbInfo.SbThumbVideoPath,
                    Version = sbInfo.Version,
                    FolderName = sbInfo.FolderName,
                    InOwnDb = sbInfo.InOwnDb
                });
            }
            else
            {
                result.SbThumbPath = sbInfo.SbThumbPath;
                result.SbThumbVideoPath = sbInfo.SbThumbVideoPath;
                result.Version = sbInfo.Version;
                result.FolderName = sbInfo.FolderName;
                result.InOwnDb = sbInfo.InOwnDb;
            }

            db.SaveChanges();
        }

        public void SetMapSbInfo(Beatmap beatmap, StoryboardInfo sbInfo)
        {
            SetMapSbInfo(beatmap.Id, sbInfo);
        }

        private void InnerUpdateMap(IMapIdentifiable id, Action<BeatmapSettings> updateAction)
        {
            if (id.IsMapTemporary())
            {
                Logger.Debug("需确认加入自定义目录后才可继续");
            }

            try
            {
                using var db = CreateDbContext();
                var map = db.BeatmapSettings.FirstOrDefault(k =>
                    k.Version == id.Version &&
                    k.FolderName == id.FolderName &&
                    k.InOwnDb == id.InOwnDb);

                if (map == null)
                {
                    map = new BeatmapSettings
                    {
                        Id = Guid.NewGuid().ToString(),
                        Version = id.Version,
                        FolderName = id.FolderName,
                        InOwnDb = id.InOwnDb,
                        Offset = 0
                    };
                    db.BeatmapSettings.Add(map);
                }

                updateAction(map);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling InnerUpdateMap().");
                throw;
            }
        }
    }
}