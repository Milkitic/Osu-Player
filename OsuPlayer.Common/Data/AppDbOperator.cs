using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Dapper;
using Milky.OsuPlayer.Common.Data.Dapper;
using Milky.OsuPlayer.Common.Data.Dapper.Provider;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Player;

namespace Milky.OsuPlayer.Common.Data
{
    public class AppDbOperator
    {
        private const string TABLE_RELATION = "collection_relation";
        private const string TABLE_MAP = "map_info";
        private const string TABLE_COLLECTION = "collection";

        static AppDbOperator()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        private static ReadOnlyDictionary<string, string> _creationMapping =
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>()
                {
                    ["collection"] = @"
CREATE TABLE collection (
    [id]          NVARCHAR (128)        NOT NULL,
    [name]        NVARCHAR (2147483647) NOT NULL,
    [locked]      INT                   NOT NULL,
    [index]       INT                   NOT NULL,
    [imagePath]   NVARCHAR (2147483647),
    [description] NVARCHAR (2147483647),
    [createTime]  DATETIME              NOT NULL,
    PRIMARY KEY (
        id
    )
);",
                    ["collection_relation"] = @"
CREATE TABLE collection_relation (
    [id]           NVARCHAR (128)        NOT NULL,
    [collectionId] NVARCHAR (2147483647) NOT NULL,
    [mapId]        NVARCHAR (2147483647) NOT NULL,
    [addTime]      DATETIME,
    PRIMARY KEY (
        id
    )
);",
                    ["map_info"] = @"
CREATE TABLE map_info (
    [id]           NVARCHAR (128)        NOT NULL,
    [version]      NVARCHAR (2147483647) NOT NULL,
    [folder]       NVARCHAR (2147483647) NOT NULL,
    [offset]       INT                   NOT NULL,
    [lastPlayTime] DATETIME,
    [exportFile]   NVARCHAR (2147483647),
    PRIMARY KEY (
        id
    )
);
PRAGMA case_sensitive_like=false;"
                });

        private static ThreadLocal<SQLiteProvider> _provider = new ThreadLocal<SQLiteProvider>(() =>
            (SQLiteProvider)new SQLiteProvider().ConfigureConnectionString("data source=player.db"));

        private static SQLiteProvider ThreadedProvider => _provider.Value;

        private List<CollectionRelation> GetCollectionsRelations()
        {
            return ThreadedProvider.Query<CollectionRelation>(TABLE_RELATION).ToList();
        }

        private List<MapInfo> GetMaps()
        {
            return ThreadedProvider.Query<MapInfo>(TABLE_MAP).ToList();
        }

        public static void ValidateDb()
        {
            var dbFile = Path.Combine(Domain.CurrentPath, "player.db");
            if (!File.Exists(dbFile))
            {
                File.WriteAllText(dbFile, "");
            }

            var tables = ThreadedProvider.GetAllTables();

            foreach (var pair in _creationMapping)
            {
                if (tables.Contains(pair.Key)) continue;
                try
                {
                    ThreadedProvider.GetDbConnection().Execute(pair.Value);
                }
                catch (Exception exc)
                {
                    throw new Exception($"创建表`{pair}`失败", exc);
                }
            }
        }

        public MapInfo GetMapFromDb(MapIdentity id)
        {
            var map = ThreadedProvider.Query<MapInfo>(TABLE_MAP,
                    new Where[]
                    {
                        ("version", id.Version),
                        ("folder", id.FolderName)
                    },
                    count: 1)
                .FirstOrDefault();

            if (map == null)
            {
                ThreadedProvider.Insert(TABLE_MAP, new Dictionary<string, object>()
                {
                    ["id"] = Guid.NewGuid().ToString(),
                    ["version"] = id.Version,
                    ["folder"] = id.FolderName,
                    ["offset"] = 0
                });
            }

            return map;
        }

        public List<MapInfo> GetRecentList()
        {
            return ThreadedProvider.Query<MapInfo>(TABLE_MAP,
                    ("lastPlayTime", null, "!="),
                    orderColumn: "lastPlayTime")
                .ToList();
        }

        public List<MapInfo> GetExportedMaps()
        {
            return ThreadedProvider.GetDbConnection()
                .Query<MapInfo>(@"SELECT * FROM map_info WHERE exportFile IS NOT NULL AND TRIM(exportFile) <> ''").ToList();
        }

        public List<MapInfo> GetMapsFromCollection(Collection collection)
        {
            return ThreadedProvider.GetDbConnection()
                .Query<MapInfo>(@"
SELECT map.id,
       map.version,
       map.folder,
       map.[offset],
       map.lastPlayTime,
       map.exportFile,
       relation.addTime
  FROM (
           SELECT *
             FROM collection_relation
            WHERE collectionId = @collectionId
       )
       AS relation
       INNER JOIN
       map_info AS map ON relation.mapId = map.id;
", new { collectionId = collection.Id }).ToList();
        }

        public List<Collection> GetCollections()
        {
            return ThreadedProvider.Query<Collection>(TABLE_COLLECTION).ToList();
        }

        public List<Collection> GetCollectionsByMap(MapInfo map)
        {
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
", new { mapId = map.Id }).ToList();
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
            return ThreadedProvider.Query(TABLE_COLLECTION, ("id", id), count: 1).FirstOrDefault();
        }

        public void AddMapsToCollection(IList<Beatmap> beatmaps, Collection collection)
        {
            var currentInfo = Services.Get<PlayerList>().CurrentInfo;
            if (beatmaps.Count < 1) return;

            var sb = new StringBuilder($"INSERT INTO {TABLE_RELATION} (id, collectionId, mapId, addTime) VALUES ");
            foreach (var beatmap in beatmaps)
            {
                sb.AppendLine($"({Guid.NewGuid().ToString()}, {collection.Id}, {beatmap.Id}, {DateTime.Now}),"); // maybe no injection here

                // todo: not suitable position
                if (currentInfo == null) continue;
                if (collection.LockedBool && currentInfo.Identity.Equals(beatmap.GetIdentity()))
                {
                    currentInfo.IsFavorite = true;
                }
            }

            sb.Remove(sb.Length - 1, 1).Append(";");
            ThreadedProvider.GetDbConnection().Execute(sb.ToString());
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
                        ["id"] = Guid.NewGuid().ToString(),
                        ["name"] = collection.Name,
                        ["locked"] = collection.LockedBool ? 1 : 0,
                        ["index"] = collection.Index,
                        ["createTime"] = collection.CreateTime
                    },
                    new Where[] { ("id", collection.Id) },
                    count: 1);
            }
        }

        public void UpdateMap(MapIdentity id, int? offset = null)
        {
            InnerUpdateMap(id, new Dictionary<string, object> { ["offset"] = offset, ["lastPlayTime"] = DateTime.Now });
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
            var map = GetMapFromDb(id);
            ThreadedProvider.Delete(TABLE_RELATION, new Where[] { ("collectionId", collection.Id), ("mapId", map.Id) });

            // todo: not suitable position
            var currentInfo = Services.Get<PlayerList>().CurrentInfo;
            if (currentInfo == null) return;
            if (collection.LockedBool && currentInfo.Identity.Equals(id))
            {
                currentInfo.IsFavorite = false;
            }
        }

        private void InnerUpdateMap(MapIdentity id, Dictionary<string, object> updateColumns)
        {
            GetMapFromDb(id);
            if (updateColumns.Count == 0) return;

            ThreadedProvider.Update(TABLE_MAP,
                updateColumns,
                new Where[]
                {
                    ("version", id.Version),
                    ("folder", id.FolderName)
                },
                count: 1);
        }
    }
}
