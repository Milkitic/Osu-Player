using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Dapper;
using Milky.OsuPlayer.Data.Dapper;
using Milky.OsuPlayer.Data.Dapper.Provider;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.Shared.Models;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.Data
{
    public class AppDbOperator
    {
        public const string TABLE_BEATMAP = "beatmap";
        private const string TABLE_RELATION = "collection_relation";
        private const string TABLE_MAP = "map_info";
        private const string TABLE_THUMB = "map_thumb";
        private const string TABLE_SB = "sb_info";
        private const string TABLE_COLLECTION = "collection";

        static AppDbOperator()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        private static ReadOnlyDictionary<string, string> _creationMapping =
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>()
                {
                    [TABLE_COLLECTION] = $@"
CREATE TABLE {TABLE_COLLECTION} (
    [id]          NVARCHAR (40)        NOT NULL,
    [name]        NVARCHAR (100) NOT NULL,
    [locked]      INT                   NOT NULL,
    [index]       INT                   NOT NULL,
    [imagePath]   NVARCHAR (700),
    [description] NVARCHAR (700),
    [createTime]  DATETIME              NOT NULL,
    PRIMARY KEY (
        id
    )
);",
                    [TABLE_RELATION] = $@"
CREATE TABLE {TABLE_RELATION} (
    [id]           NVARCHAR (40)        NOT NULL,
    [collectionId] NVARCHAR (40) NOT NULL,
    [mapId]        NVARCHAR (40) NOT NULL,
    [addTime]      DATETIME,
    PRIMARY KEY (
        id
    )
);",
                    [TABLE_MAP] = $@"
CREATE TABLE {TABLE_MAP} (
    [id]           NVARCHAR (40)        NOT NULL,
    [version]      NVARCHAR (255) NOT NULL,
    [folder]       NVARCHAR (255) NOT NULL,
    [ownDb]          BIT NOT NULL,
    [offset]       INT                   NOT NULL,
    [lastPlayTime] DATETIME,
    [exportFile]   NVARCHAR (700),
    PRIMARY KEY (
        id
    )
);
PRAGMA case_sensitive_like=false;",
                    [TABLE_THUMB] = $@"
CREATE TABLE {TABLE_THUMB} (
    [id]           NVARCHAR (40) PRIMARY KEY
                                         NOT NULL,
    [mapId]        NVARCHAR (40) NOT NULL,
    [thumbPath]    NVARCHAR (40) NOT NULL
);
",
                    [TABLE_SB] = $@"
CREATE TABLE {TABLE_SB} (
    [id]               NVARCHAR (40) PRIMARY KEY
                                         NOT NULL,
    [mapId]            NVARCHAR (40) NOT NULL,
    [thumbPath]        NVARCHAR (40) NOT NULL,
    [thumbVideoPath]   NVARCHAR (40) NOT NULL,
    [version]          NVARCHAR (255) NOT NULL,
    [folder]           NVARCHAR (255) NOT NULL,
    [own]              BIT NOT NULL
);
",
                    [TABLE_BEATMAP] = $@"
CREATE TABLE {TABLE_BEATMAP} (
    id            UNIQUEIDENTIFIER      NOT NULL,
    artist        NVARCHAR (2147483647),
    artistU       NVARCHAR (2147483647),
    title         NVARCHAR (2147483647),
    titleU        NVARCHAR (2147483647),
    creator       NVARCHAR (2147483647),
    version       NVARCHAR (2147483647),
    fileName      NVARCHAR (2147483647),
    lastModified  DATETIME              NOT NULL,
    diffSrStd     FLOAT                 NOT NULL,
    diffSrTaiko   FLOAT                 NOT NULL,
    diffSrCtb     FLOAT                 NOT NULL,
    diffSrMania   FLOAT                 NOT NULL,
    drainTime     INT                   NOT NULL,
    totalTime     INT                   NOT NULL,
    audioPreview  INT                   NOT NULL,
    beatmapId     INT                   NOT NULL,
    beatmapSetId  INT                   NOT NULL,
    gameMode      INT                   NOT NULL,
    source        NVARCHAR (2147483647),
    tags          NVARCHAR (2147483647),
    folderName    NVARCHAR (2147483647),
    audioName     NVARCHAR (2147483647),
    own           BIT                   NOT NULL,
    PRIMARY KEY (
        id
    )
);
PRAGMA case_sensitive_like=false;"
                });

        private static ThreadLocal<SQLiteProvider> _provider = new ThreadLocal<SQLiteProvider>(() =>
            (SQLiteProvider)new SQLiteProvider().ConfigureConnectionString("data source=player.db"));

        public SQLiteProvider ThreadedProvider => _provider.Value;

        private List<CollectionRelation> GetCollectionsRelations()
        {
            return ThreadedProvider.Query<CollectionRelation>(TABLE_RELATION).ToList();
        }

        private List<BeatmapSettings> GetMaps()
        {
            return ThreadedProvider.Query<BeatmapSettings>(TABLE_MAP).ToList();
        }

        public static void ValidateDb()
        {
            var dbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "player.db");
            if (!File.Exists(dbFile))
            {
                File.WriteAllText(dbFile, "");
            }

            var prov = _provider.Value;

            var tables = prov.GetAllTables();

            foreach (var pair in _creationMapping)
            {
                if (tables.Contains(pair.Key)) continue;
                try
                {
                    prov.GetDbConnection().Execute(pair.Value);
                }
                catch (Exception exc)
                {
                    throw new Exception($"创建表`{pair}`失败", exc);
                }
            }
        }

        public BeatmapSettings GetMapFromDb(MapIdentity id)
        {
            if (id.IsMapTemporary())
            {
                throw new NotImplementedException("需确认加入自定义目录后才可继续");
            }

            var map = ThreadedProvider.Query<BeatmapSettings>(TABLE_MAP,
                    new Where[]
                    {
                        ("version", id.Version),
                        ("folder", id.FolderName),
                        ("ownDb", id.InOwnDb)
                    },
                    count: 1)
                .FirstOrDefault();

            if (map == null)
            {
                var guid = Guid.NewGuid().ToString();
                ThreadedProvider.Insert(TABLE_MAP, new Dictionary<string, object>()
                {
                    ["id"] = guid,
                    ["version"] = id.Version,
                    ["folder"] = id.FolderName,
                    ["ownDb"] = id.InOwnDb,
                    ["offset"] = 0
                });

                return new BeatmapSettings
                {
                    Id = guid,
                    Version = id.Version,
                    FolderName = id.FolderName,
                    InOwnDb = id.InOwnDb,
                    Offset = 0
                };
            }

            return map;
        }

        public List<BeatmapSettings> GetRecentList()
        {
            return ThreadedProvider.Query<BeatmapSettings>(TABLE_MAP,
                    ("lastPlayTime", null, "!="),
                    orderColumn: "lastPlayTime")
                .ToList();
        }

        public List<BeatmapSettings> GetExportedMaps()
        {
            return ThreadedProvider.GetDbConnection()
                .Query<BeatmapSettings>(@"SELECT * FROM map_info WHERE exportFile IS NOT NULL AND TRIM(exportFile) <> ''").ToList();
        }

        public List<BeatmapSettings> GetMapsFromCollection(Collection collection)
        {
            var result = ThreadedProvider.GetDbConnection()
                .Query<BeatmapSettings>(@"
SELECT map.id,
       map.version,
       map.folder,
       map.ownDb,
       map.[offset],
       map.lastPlayTime,
       map.exportFile,
       CAST (relation.addTime AS VARCHAR (30) ) AS addTime
  FROM (
           SELECT *
             FROM collection_relation
            WHERE collectionId = @collectionId
       )
       AS relation
       INNER JOIN
       map_info AS map ON relation.mapId = map.id;
", new { collectionId = collection.Id }).ToList();
            return result;
        }

        public List<Collection> GetCollections()
        {
            return ThreadedProvider.Query<Collection>(TABLE_COLLECTION).ToList();
        }

        public List<Collection> GetCollectionsByMap(BeatmapSettings beatmapSettings)
        {
            if (beatmapSettings.IsMapTemporary())
            {
                throw new NotImplementedException("需确认加入自定义目录后才可继续");
            }

            return ThreadedProvider.GetDbConnection()
                .Query<Collection>(@"
SELECT collection.id,
       collection.name,
       collection.locked,
       collection.[index],
       collection.imagePath,
       collection.description,
       collection.createTime
  FROM (
           SELECT *
             FROM collection_relation
            WHERE mapId = @mapId
       )
       AS relation
       INNER JOIN
       collection ON relation.collectionId = collection.id;
", new { mapId = beatmapSettings.Id }).ToList();
        }

        public void AddCollection(string name, bool locked = false)
        {
            ThreadedProvider.Insert(TABLE_COLLECTION, new Dictionary<string, object>()
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["name"] = name,
                ["locked"] = locked ? 1 : 0,
                ["index"] = 0,
                ["createTime"] = DateTime.Now
            });
        }

        private void AddCollection(Collection collection)
        {
            ThreadedProvider.Insert(TABLE_COLLECTION, new Dictionary<string, object>
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["name"] = collection.Name,
                ["locked"] = collection.LockedBool ? 1 : 0,
                ["index"] = collection.Index,
                ["createTime"] = collection.CreateTime
            });
        }

        public Collection GetCollectionById(string id)
        {
            return ThreadedProvider.Query<Collection>(TABLE_COLLECTION, ("id", id), count: 1).FirstOrDefault();
        }

        //todo: 添加时有误
        public void AddMapsToCollection(IList<Beatmap> beatmaps, Collection collection)
        {
            if (beatmaps.Count < 1) return;

            var sb = new StringBuilder($"INSERT INTO {TABLE_RELATION} (id, collectionId, mapId, addTime) VALUES ");
            foreach (var beatmap in beatmaps)
            {
                if (beatmap.IsMapTemporary())
                {
                    throw new NotImplementedException("需确认加入自定义目录后才可继续");
                }

                var map = GetMapFromDb(beatmap.GetIdentity());
                sb.Append($"('{Guid.NewGuid()}', '{collection.Id}', '{map.Id}', '{DateTime.Now}'),"); // maybe no injection here
            }

            sb.Remove(sb.Length - 1, 1).Append(";");
            var sql = sb.ToString();
            ThreadedProvider.GetDbConnection().Execute(sql);
        }

        public void UpdateCollection(Collection collection)
        {
            var result = GetCollectionById(collection.Id);
            if (result == null)
            {
                AddCollection(collection);
            }
            else
            {
                ThreadedProvider.Update(TABLE_COLLECTION,
                    new Dictionary<string, object>
                    {
                        //["id"] = Guid.NewGuid().ToString(),
                        ["name"] = collection.Name,
                        ["locked"] = collection.LockedBool ? 1 : 0,
                        ["index"] = collection.Index,
                        ["imagePath"] = collection.ImagePath,
                        ["description"] = collection.Description,
                        ["createTime"] = collection.CreateTime
                    },
                    new Where[] { ("id", collection.Id) },
                    count: 1);
            }
        }

        public void UpdateMap(MapIdentity id, int? offset = null)
        {
            var updateColumns = new Dictionary<string, object> { ["lastPlayTime"] = DateTime.Now };
            if (offset != null)
            {
                updateColumns.Add("offset", offset);
            }

            InnerUpdateMap(id, updateColumns);
        }

        public void AddMapExport(MapIdentity id, string exportFilePath)
        {
            InnerUpdateMap(id, new Dictionary<string, object> { ["exportFile"] = exportFilePath });
        }

        public void RemoveMapExport(MapIdentity id)
        {
            InnerUpdateMap(id, new Dictionary<string, object> { ["exportFile"] = null });
        }

        public void UpdateMap(Beatmap beatmap, int? offset = null)
        {
            UpdateMap(beatmap.GetIdentity(), offset);
        }

        public void RemoveFromRecent(MapIdentity id)
        {
            InnerUpdateMap(id, new Dictionary<string, object> { ["lastPlayTime"] = null });
        }

        public void RemoveFromRecent(Beatmap beatmap)
        {
            RemoveFromRecent(beatmap.GetIdentity());
        }

        public void ClearRecent()
        {
            ThreadedProvider.Update(TABLE_COLLECTION,
                new Dictionary<string, object>
                {
                    ["lastPlayTime"] = null
                },
                Array.Empty<Where>());
        }

        public void RemoveCollection(Collection collection)
        {
            ThreadedProvider.Delete(TABLE_COLLECTION, ("id", collection.Id));
            ThreadedProvider.Delete(TABLE_RELATION, ("collectionId", collection.Id));
        }
        public void RemoveMapFromCollection(Beatmap beatmap, Collection collection)
        {
            RemoveMapFromCollection(beatmap.GetIdentity(), collection);
        }

        public void RemoveMapFromCollection(MapIdentity id, Collection collection)
        {
            if (id.IsMapTemporary())
            {
                throw new NotImplementedException("需确认加入自定义目录后才可继续");
            }

            var map = GetMapFromDb(id);
            ThreadedProvider.Delete(TABLE_RELATION, new Where[] { ("collectionId", collection.Id), ("mapId", map.Id) });
        }

        public bool GetMapThumb(Guid beatmapDbId, out string thumbPath)
        {
            var dy = ThreadedProvider.Query(TABLE_THUMB,
                ("mapId", beatmapDbId, "=="),
                count: 1).FirstOrDefault();
            thumbPath = dy?.thumbPath;
            return !(dy is null);
        }

        public bool GetMapThumb(Beatmap beatmap, out string thumbPath)
        {
            if (beatmap.IsMapTemporary())
            {
                throw new NotImplementedException("需确认加入自定义目录后才可继续");
            }

            return GetMapThumb(beatmap.Id, out thumbPath);
        }

        public void SetMapThumb(Guid beatmapDbId, string thumbPath)
        {
            var hasResult = GetMapThumb(beatmapDbId, out _);

            if (hasResult)
            {
                ThreadedProvider.Update(TABLE_THUMB,
                    new Dictionary<string, object>
                    {
                        ["thumbPath"] = thumbPath
                    },
                    ("mapId", beatmapDbId, "=="));
            }
            else
            {
                ThreadedProvider.Insert(TABLE_THUMB,
                    new Dictionary<string, object>
                    {
                        ["id"] = Guid.NewGuid().ToString(),
                        ["mapId"] = beatmapDbId,
                        ["thumbPath"] = thumbPath
                    }
                );
            }
        }

        public void SetMapThumb(Beatmap beatmap, string thumbPath)
        {
            SetMapThumb(beatmap.Id, thumbPath);
        }

        public void SetMapSbInfo(Guid beatmapDbId, StoryboardInfo sbInfo)
        {
            if (sbInfo.IsMapTemporary())
            {
                throw new NotImplementedException("需确认加入自定义目录后才可继续");
            }

            var hasResult = GetMapThumb(beatmapDbId, out _);

            if (hasResult)
            {
                ThreadedProvider.Update(TABLE_SB,
                    new Dictionary<string, object>
                    {
                        ["thumbPath"] = sbInfo.SbThumbPath,
                        ["thumbVideoPath"] = sbInfo.SbThumbVideoPath,
                        ["version"] = sbInfo.Version,
                        ["folder"] = sbInfo.FolderName,
                        ["ownDb"] = sbInfo.InOwnDb
                    },
                    ("mapId", beatmapDbId, "=="));
            }
            else
            {
                ThreadedProvider.Insert(TABLE_SB,
                    new Dictionary<string, object>
                    {
                        ["id"] = Guid.NewGuid().ToString(),
                        ["mapId"] = beatmapDbId,
                        ["thumbPath"] = sbInfo.SbThumbPath,
                        ["thumbVideoPath"] = sbInfo.SbThumbVideoPath,
                        ["version"] = sbInfo.Version,
                        ["folder"] = sbInfo.FolderName,
                        ["ownDb"] = sbInfo.InOwnDb
                    }
                );
            }
        }

        public void SetMapSbInfo(Beatmap beatmap, StoryboardInfo sbInfo)
        {
            SetMapSbInfo(beatmap.Id, sbInfo);
        }

        private void InnerUpdateMap(MapIdentity id, Dictionary<string, object> updateColumns)
        {
            if (id.IsMapTemporary())
            {
                throw new NotImplementedException("需确认加入自定义目录后才可继续");
            }

            GetMapFromDb(id);
            if (updateColumns.Count == 0) return;

            ThreadedProvider.Update(TABLE_MAP,
                updateColumns,
                new Where[]
                {
                    ("version", id.Version),
                    ("folder", id.FolderName),
                    ("ownDb", id.InOwnDb)
                },
                count: 1);
        }
    }
}
