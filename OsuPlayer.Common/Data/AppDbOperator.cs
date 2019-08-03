using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Player;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Collection = Milky.OsuPlayer.Common.Data.EF.Model.V1.Collection;

namespace Milky.OsuPlayer.Common.Data
{
    public class AppDbOperator
    {
        [ThreadStatic]
        private static ApplicationDbContext _ctx;

        private ApplicationDbContext Ctx
        {
            get
            {
                if (_ctx == null || _ctx.IsDisposed())
                    _ctx = new ApplicationDbContext();
                return _ctx;
            }
        }

        private List<CollectionRelation> GetCollectionsRelations()
        {
            return Ctx.Relations.ToList();
        }

        private List<MapInfo> GetMaps()
        {

            return Ctx.MapInfos.ToList();

        }

        public MapInfo GetMapFromDb(MapIdentity id)
        {

            return InnerGetMapFromDb(id, Ctx);

        }

        public List<MapInfo> GetRecentList()
        {
            return Ctx.MapInfos
                .Where(k => k.LastPlayTime != null)
                .OrderBy(k => k.LastPlayTime)
                .ToList();
        }

        public List<MapInfo> GetExportedMaps()
        {
            return Ctx.MapInfos
                .Where(k => !string.IsNullOrEmpty(k.ExportFile))
                .ToList();
        }

        public List<MapInfo> GetMapsFromCollection(Collection collection)
        {
            var mapRelations = Ctx.Relations.Where(k => k.CollectionId == collection.Id).ToList();
            var mapIds = mapRelations.Select(k => k.MapId).ToList();
            var maps = Ctx.MapInfos.Where(k => mapIds.Contains(k.Id)).ToList();
            return mapRelations.Join(maps,
                    r => r.MapId,
                    m => m.Id,
                    (r, m) => new MapInfo(
                        m.Id,
                        m.Version,
                        m.FolderName,
                        m.Offset,
                        m.LastPlayTime,
                        m.ExportFile,
                        r.AddTime)
                )
                .ToList();
        }

        public List<Collection> GetCollections()
        {
            return Ctx.Collections.ToList();
        }

        public List<Collection> GetCollectionById(string[] id)
        {
            return Ctx.Collections.Where(k => id.Contains(k.Id)).ToList();
        }

        public Collection GetCollectionById(string id)
        {
            return Ctx.Collections.FirstOrDefault(k => id.Contains(k.Id));
        }

        public List<Collection> GetCollectionsByMap(MapInfo map)
        {
            var ids = Ctx.Relations.Where(k => k.MapId == map.Id).Select(k => k.CollectionId);
            if (!ids.Any()) return null;
            return GetCollectionById(ids.ToArray());
        }

        public void AddCollection(string name, bool locked = false)
        {
            var newOne = new Collection(Guid.NewGuid().ToString(), name, locked, 0) { CreateTime = DateTime.Now };
            Ctx.Collections.Add(newOne);
            Ctx.SaveChanges();
        }

        public void AddMapToCollection(Beatmap beatmap, Collection collection)
        {
            MapInfo map = InnerGetMapFromDb(beatmap.GetIdentity(), Ctx);
            Ctx.Relations.Add(new CollectionRelation(Guid.NewGuid().ToString(), collection.Id,
                map.Id));
            Ctx.SaveChanges();

            //todo: not suitable position
            var currentInfo = Services.Get<PlayerList>().CurrentInfo;
            if (currentInfo != null)
            {
                if (collection.Locked &&
                    currentInfo.Identity.Equals(beatmap.GetIdentity()))
                {
                    currentInfo.IsFavorite = true;
                }
            }
        }

        public void UpdateCollection(Collection collection)
        {
            Collection col = Ctx.Collections.FirstOrDefault(k =>
                 k.Id == collection.Id);
            if (col == null)
            {
                var newOne = new Collection(Guid.NewGuid().ToString(), collection.Name, false, 0,
                    collection.ImagePath, collection.Description)
                {
                    CreateTime = DateTime.Now
                };
                Ctx.Collections.Add(newOne);
            }
            else
            {
                col.Description = collection.Description;
                col.ImagePath = collection.ImagePath;
                col.Index = collection.Index;
                col.Name = collection.Name;
            }

            Ctx.SaveChanges();
        }

        public void UpdateMap(MapIdentity id, int offset = 0)
        {
            MapInfo map = InnerGetMapFromDb(id, Ctx);
            map.LastPlayTime = DateTime.Now;
            if (offset != 0)
                map.Offset = offset;
            Ctx.SaveChanges();
        }

        public void AddMapExport(MapIdentity id, string exportFilePath)
        {
            MapInfo map = InnerGetMapFromDb(id, Ctx);
            map.ExportFile = exportFilePath;
            Ctx.SaveChanges();
        }

        public void RemoveMapExport(MapIdentity id)
        {
            MapInfo map = InnerGetMapFromDb(id, Ctx);
            map.ExportFile = string.Empty;
            Ctx.SaveChanges();

        }

        public void UpdateMap(Beatmap beatmap, int offset)
        {
            UpdateMap(beatmap.GetIdentity());
        }

        public void RemoveFromRecent(MapIdentity id)
        {
            MapInfo map = InnerGetMapFromDb(id, Ctx);
            map.LastPlayTime = null;
            Ctx.SaveChanges();
        }

        public void RemoveFromRecent(Beatmap beatmap)
        {
            RemoveFromRecent(beatmap.GetIdentity());
        }

        public void ClearRecent()
        {
            var maps = Ctx.MapInfos;
            foreach (var map in maps)
                map.LastPlayTime = null;
            Ctx.SaveChanges();
        }

        public void RemoveCollection(Collection collection)
        {
            var coll = Ctx.Collections.FirstOrDefault(k => k.Id == collection.Id);
            if (coll != null) Ctx.Collections.Remove(coll);
            Ctx.Relations.RemoveRange(Ctx.Relations.Where(k => k.CollectionId == collection.Id));

            Ctx.SaveChanges();
        }

        public void RemoveMapFromCollection(Beatmap beatmap, Collection collection)
        {
            RemoveMapFromCollection(beatmap.GetIdentity(), collection);
        }

        public void RemoveMapFromCollection(MapIdentity id, Collection collection)
        {
            MapInfo map = InnerGetMapFromDb(id, Ctx);
            Ctx.Relations.RemoveRange(Ctx.Relations.Where(k =>
                k.CollectionId == collection.Id && k.MapId == map.Id));
            Ctx.SaveChanges();

            //todo: not suitable position
            var currentInfo = Services.Get<PlayerList>().CurrentInfo;
            if (currentInfo != null)
            {
                if (collection.Locked &&
                    currentInfo.Identity.Equals(id))
                {
                    currentInfo.IsFavorite = false;
                }
            }
        }

        private static MapInfo InnerGetMapFromDb(MapIdentity id, ApplicationDbContext context)
        {
            var map = context.MapInfos.FirstOrDefault(k =>
                k.Version == id.Version && k.FolderName == id.FolderName);
            if (map == null)
            {
                map = new MapInfo(Guid.NewGuid().ToString(), id.Version, id.FolderName, 0, null);
                context.MapInfos.Add(map);
                context.SaveChanges();
            }

            return map;
        }
        
        private static DbJsonObject _dbJsonObject;
        private static AppDbOperator _opt;

        static AppDbOperator()
        {
            _opt = new AppDbOperator();
            LookupBackup();
        }

        private static void LookupBackup()
        {
            Task.Run(() =>
            {
                _dbJsonObject = new DbJsonObject
                {
                    Collections = _opt.GetCollections(),
                    MapInfos = _opt.GetMaps(),
                    CollectionRelations = _opt.GetCollectionsRelations()
                };

                Backup();
            });
        }

        private static void Backup()
        {
            ConcurrentFile.WriteAllText("player.db.bak", JsonConvert.SerializeObject(_dbJsonObject));
        }
    }
}
