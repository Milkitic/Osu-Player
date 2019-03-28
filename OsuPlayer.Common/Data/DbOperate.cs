using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Player;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collection = Milky.OsuPlayer.Common.Data.EF.Model.V1.Collection;

namespace Milky.OsuPlayer.Common.Data
{
    public static class DbOperate
    {
        private static DbJsonObject _dbJsonObject;

        static DbOperate()
        {
            LookupBackup();
        }

        private static void LookupBackup()
        {
            Task.Run(() =>
            {
                _dbJsonObject = new DbJsonObject
                {
                    Collections = GetCollections(),
                    MapInfos = GetMaps(),
                    CollectionRelations = GetCollectionsRelations()
                };

                Backup();
            });
        }

        private static void Backup()
        {
            ConcurrentFile.WriteAllText("player.db.bak", JsonConvert.SerializeObject(_dbJsonObject));
        }

        private static List<CollectionRelation> GetCollectionsRelations()
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    return context.Relations.ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        private static List<MapInfo> GetMaps()
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    return context.MapInfos.ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static MapInfo GetMapFromDb(MapIdentity id)
        {
            using (ApplicationDbContext mapContext = new ApplicationDbContext())
            {
                try
                {
                    return InnerGetMapFromDb(id, mapContext);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static List<MapInfo> GetRecent()
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    return context.MapInfos.Where(k => k.LastPlayTime != null).OrderBy(k => k.LastPlayTime)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static List<MapInfo> GetExportedMaps()
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    return context.MapInfos.Where(k => !string.IsNullOrEmpty(k.ExportFile)).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static List<MapInfo> GetMapsFromCollection(Collection collection)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    var mapRelations = context.Relations.Where(k => k.CollectionId == collection.Id).ToList();
                    var mapIds = mapRelations.Select(k => k.MapId).ToList();
                    var maps = context.MapInfos.Where(k => mapIds.Contains(k.Id)).ToList();
                    return (from r in mapRelations
                            join m in maps on r.MapId equals m.Id
                            select new MapInfo(m.Id, m.Version, m.FolderName, m.Offset, m.LastPlayTime, m.ExportFile, r.AddTime))
                        .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static List<Collection> GetCollections()
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    return context.Collections.ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static List<Collection> GetCollectionById(string[] id)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    return context.Collections.Where(k => id.Contains(k.Id)).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static Collection GetCollectionById(string id)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    return context.Collections.FirstOrDefault(k => id.Contains(k.Id));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static List<Collection> GetCollectionsByMap(MapInfo map)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    var ids = context.Relations.Where(k => k.MapId == map.Id).Select(k => k.CollectionId);
                    if (!ids.Any()) return null;
                    return GetCollectionById(ids.ToArray());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void AddCollection(string name, bool locked = false)
        {
            using (ApplicationDbContext applicationDbContext = new ApplicationDbContext())
            {
                try
                {
                    var newOne = new Collection(Guid.NewGuid().ToString(), name, locked, 0) { CreateTime = DateTime.Now };
                    applicationDbContext.Collections.Add(newOne);
                    applicationDbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void AddMapToCollection(Beatmap beatmap, Collection collection)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    MapInfo map = InnerGetMapFromDb(beatmap.GetIdentity(), context);
                    context.Relations.Add(new CollectionRelation(Guid.NewGuid().ToString(), collection.Id,
                        map.Id));
                    context.SaveChanges();

                    //todo: not suitable position
                    var currentInfo = InstanceManage.GetInstance<PlayerList>().CurrentInfo;
                    if (currentInfo != null)
                    {
                        if (collection.Locked &&
                            currentInfo.Identity.Equals(beatmap.GetIdentity()))
                        {
                            currentInfo.IsFavorite = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void UpdateCollection(Collection collection)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    Collection col = context.Collections.FirstOrDefault(k =>
                         k.Id == collection.Id);
                    if (col == null)
                    {
                        var newOne = new Collection(Guid.NewGuid().ToString(), collection.Name, false, 0,
                            collection.ImagePath, collection.Description)
                        {
                            CreateTime = DateTime.Now
                        };
                        context.Collections.Add(newOne);
                    }
                    else
                    {
                        col.Description = collection.Description;
                        col.ImagePath = collection.ImagePath;
                        col.Index = collection.Index;
                        col.Name = collection.Name;
                    }

                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void UpdateMap(MapIdentity id, int offset = 0)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    MapInfo map = InnerGetMapFromDb(id, context);
                    map.LastPlayTime = DateTime.Now;
                    if (offset != 0)
                        map.Offset = offset;
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void AddMapExport(MapIdentity id, string exportFilePath)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    MapInfo map = InnerGetMapFromDb(id, context);
                    map.ExportFile = exportFilePath;
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void RemoveMapExport(MapIdentity id)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    MapInfo map = InnerGetMapFromDb(id, context);
                    map.ExportFile = string.Empty;
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void UpdateMap(Beatmap beatmap, int offset)
        {
            UpdateMap(beatmap.GetIdentity());
        }

        public static void RemoveFromRecent(MapIdentity id)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                MapInfo map = InnerGetMapFromDb(id, context);
                map.LastPlayTime = null;
                context.SaveChanges();
            }
        }

        public static void RemoveFromRecent(Beatmap beatmap)
        {
            RemoveFromRecent(beatmap.GetIdentity());
        }

        public static void ClearRecent()
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                var maps = context.MapInfos;
                foreach (var map in maps)
                    map.LastPlayTime = null;
                context.SaveChanges();
            }
        }

        public static void RemoveCollection(Collection collection)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    var coll = context.Collections.FirstOrDefault(k => k.Id == collection.Id);
                    if (coll != null) context.Collections.Remove(coll);
                    context.Relations.RemoveRange(context.Relations.Where(k => k.CollectionId == collection.Id));

                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void RemoveMapFromCollection(Beatmap beatmap, Collection collection)
        {
            RemoveMapFromCollection(beatmap.GetIdentity(), collection);
        }

        public static void RemoveMapFromCollection(MapIdentity id, Collection collection)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                try
                {
                    MapInfo map = InnerGetMapFromDb(id, context);
                    context.Relations.RemoveRange(context.Relations.Where(k =>
                        k.CollectionId == collection.Id && k.MapId == map.Id));
                    context.SaveChanges();

                    //todo: not suitable position
                    var currentInfo = InstanceManage.GetInstance<PlayerList>().CurrentInfo;
                    if (currentInfo != null)
                    {
                        if (collection.Locked &&
                            currentInfo.Identity.Equals(id))
                        {
                            currentInfo.IsFavorite = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        private static MapInfo InnerGetMapFromDb(MapIdentity id, ApplicationDbContext context)
        {
            var map = context.MapInfos.FirstOrDefault(k =>
                k.Version == id.Version && k.FolderName == id.FolderName);
            if (map == null)
            {
                context.MapInfos.Add(new MapInfo(Guid.NewGuid().ToString(), id.Version, id.FolderName, 0, null));
                context.SaveChanges();
                map = context.MapInfos.First(k => k.Version == id.Version && k.FolderName == id.FolderName);
            }

            return map;
        }
    }
}
