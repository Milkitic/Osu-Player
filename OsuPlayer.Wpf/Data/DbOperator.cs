using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Milkitic.OsuPlayer;
using osu_database_reader.Components.Beatmaps;

namespace Milkitic.OsuPlayer.Data
{
    public static class DbOperator
    {

        public static MapInfo GetMapFromDb(MapIdentity id)
        {
            using (MapInfoContext mapContext = new MapInfoContext())
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

        public static IEnumerable<MapInfo> GetRecent()
        {
            #region
            //    using (CollectionContext c1 = new CollectionContext())
            //    using (RelationContext c2 = new RelationContext())
            //    using (MapInfoContext c3 = new MapInfoContext())
            //    {
            //        c1.Collections.RemoveRange(c1.Collections);
            //        c2.Relations.RemoveRange(c2.Relations);
            //        c3.MapInfos.RemoveRange(c3.MapInfos);
            //        c1.SaveChanges();
            //        c2.SaveChanges();
            //        c3.SaveChanges();
            //    }
            #endregion
            using (MapInfoContext mapContext = new MapInfoContext())
            {
                try
                {
                    return mapContext.MapInfos.Where(k => k.LastPlayTime != null).OrderBy(k => k.LastPlayTime)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static IEnumerable<MapInfo> GetExportedMaps()
        {
            using (MapInfoContext mapContext = new MapInfoContext())
            {
                try
                {
                    return mapContext.MapInfos.Where(k => !string.IsNullOrEmpty(k.ExportFile)).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static IEnumerable<MapInfo> GetMapsFromCollection(Collection collection)
        {
            using (MapInfoContext mapContext = new MapInfoContext())
            using (RelationContext relationContext = new RelationContext())
            {
                try
                {
                    var mapIds = relationContext.Relations.Where(k => k.CollectionId == collection.Id)
                        .Select(k => k.MapId).ToList();
                    return mapContext.MapInfos.Where(k => mapIds.Contains(k.Id)).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static IEnumerable<Collection> GetCollections()
        {
            using (CollectionContext context = new CollectionContext())
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

        public static IEnumerable<Collection> GetCollectionById(string[] id)
        {
            using (CollectionContext context = new CollectionContext())
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

        public static IEnumerable<Collection> GetCollectionsByMap(MapInfo map)
        {
            using (RelationContext relContext = new RelationContext())
            {
                try
                {
                    var ids = relContext.Relations.Where(k => k.MapId == map.Id).Select(k => k.CollectionId);
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
            using (CollectionContext collectionContext = new CollectionContext())
            {
                try
                {
                    collectionContext.Collections.Add(new Collection(Guid.NewGuid().ToString(), name, locked, 0));
                    collectionContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void AddMapToCollection(BeatmapEntry entry, Collection collection)
        {
            using (MapInfoContext mapContext = new MapInfoContext())
            using (RelationContext relationContext = new RelationContext())
            {
                try
                {
                    MapInfo map = InnerGetMapFromDb(entry.GetIdentity(), mapContext);
                    relationContext.Relations.Add(new CollectionRelation(Guid.NewGuid().ToString(), collection.Id,
                        map.Id));
                    relationContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void UpdateMap(MapIdentity id)
        {
            if (!Directory.Exists(Path.Combine(Domain.OsuSongPath, id.FolderName)))
                throw new DirectoryNotFoundException(id.FolderName);

            using (MapInfoContext mapContext = new MapInfoContext())
            {
                try
                {
                    MapInfo map = InnerGetMapFromDb(id, mapContext);
                    map.LastPlayTime = DateTime.Now;
                    mapContext.SaveChanges();
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
            using (MapInfoContext mapContext = new MapInfoContext())
            {
                try
                {
                    MapInfo map = InnerGetMapFromDb(id, mapContext);
                    map.ExportFile = exportFilePath;
                    mapContext.SaveChanges();
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
            using (MapInfoContext mapContext = new MapInfoContext())
            {
                try
                {
                    MapInfo map = InnerGetMapFromDb(id, mapContext);
                    map.ExportFile = string.Empty;
                    mapContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void UpdateMap(BeatmapEntry entry)
        {
            UpdateMap(entry.GetIdentity());
        }

        public static void RemoveFromRecent(MapIdentity id)
        {
            using (MapInfoContext mapContext = new MapInfoContext())
            {
                MapInfo map = InnerGetMapFromDb(id, mapContext);
                map.LastPlayTime = null;
                mapContext.SaveChanges();
            }
        }

        public static void RemoveFromRecent(BeatmapEntry entry)
        {
            RemoveFromRecent(entry.GetIdentity());
        }

        public static void ClearRecent()
        {
            using (MapInfoContext mapContext = new MapInfoContext())
            {
                var maps = mapContext.MapInfos;
                foreach (var map in maps)
                    map.LastPlayTime = null;
                mapContext.SaveChanges();
            }
        }

        public static void RemoveCollection(Collection collection)
        {
            using (CollectionContext collectionContext = new CollectionContext())
            {
                try
                {
                    var sb = collectionContext.Collections.FirstOrDefault(k => k.Id == collection.Id);
                    if (sb != null)
                    {
                        collectionContext.Collections.Remove(sb);
                        collectionContext.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }

            using (RelationContext relationContext = new RelationContext())
            {
                try
                {
                    relationContext.Relations.RemoveRange(
                       relationContext.Relations.Where(k => k.CollectionId == collection.Id));
                    relationContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void RemoveMapFromCollection(BeatmapEntry entry, Collection collection)
        {
            RemoveMapFromCollection(entry.GetIdentity(), collection);
        }

        public static void RemoveMapFromCollection(MapIdentity id, Collection collection)
        {
            using (MapInfoContext mapContext = new MapInfoContext())
            using (RelationContext relationContext = new RelationContext())
            {
                try
                {
                    MapInfo map = InnerGetMapFromDb(id, mapContext);
                    relationContext.Relations.RemoveRange(relationContext.Relations.Where(k =>
                        k.CollectionId == collection.Id && k.MapId == map.Id));
                    relationContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        private static MapInfo InnerGetMapFromDb(MapIdentity id, MapInfoContext mapContext)
        {
            var map = mapContext.MapInfos.FirstOrDefault(k =>
                k.Version == id.Version && k.FolderName == id.FolderName);
            if (map == null)
            {
                mapContext.MapInfos.Add(new MapInfo(Guid.NewGuid().ToString(), id.Version, id.FolderName, 0, null));
                mapContext.SaveChanges();
                map = mapContext.MapInfos.First(k => k.Version == id.Version && k.FolderName == id.FolderName);
            }

            return map;
        }

    }
}
