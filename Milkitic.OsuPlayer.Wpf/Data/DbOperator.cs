using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Wpf.Data
{
    public static class DbOperator
    {
        public static MapInfo GetMapFromDb(string version, string folderName)
        {
            using (MapInfoContext mapContext = new MapInfoContext())
            {
                try
                {
                    return GetMapFromDb(version, folderName, mapContext);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
            }
        }

        public static void UpdateMap(string version, string folderName)
        {
            if (!Directory.Exists(Path.Combine(Domain.OsuSongPath, folderName)))
                throw new DirectoryNotFoundException(folderName);

            using (MapInfoContext mapContext = new MapInfoContext())
            {
                try
                {
                    MapInfo map = GetMapFromDb(version, folderName, mapContext);
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

        public static void UpdateMap(BeatmapEntry entry)
        {
            UpdateMap(entry.Version, entry.FolderName);
        }

        public static void RemoveFromRecent(string version, string folderName)
        {
            using (MapInfoContext mapContext = new MapInfoContext())
            {
                MapInfo map = GetMapFromDb(version, folderName, mapContext);
                map.LastPlayTime = null;
                mapContext.SaveChanges();
            }
        }

        public static void RemoveFromRecent(BeatmapEntry entry)
        {
            RemoveFromRecent(entry.Version, entry.FolderName);
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

        public static void AddCollection(string name)
        {
            using (CollectionContext collectionContext = new CollectionContext())
            {
                try
                {
                    collectionContext.Collections.Add(new Collection(Guid.NewGuid().ToString(), name));
                    collectionContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"Failed: " + ex);
                    throw;
                }
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

        public static void AddMapToCollection(BeatmapEntry entry, Collection collection)
        {
            using (MapInfoContext mapContext = new MapInfoContext())
            using (RelationContext relationContext = new RelationContext())
            {
                try
                {
                    MapInfo map = GetMapFromDb(entry.Version, entry.FolderName, mapContext);
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

        public static void RemoveMapFromCollection(BeatmapEntry entry, Collection collection)
        {
            RemoveMapFromCollection(entry.Version, entry.FolderName, collection);
        }

        public static void RemoveMapFromCollection(string version, string folderName, Collection collection)
        {
            using (MapInfoContext mapContext = new MapInfoContext())
            using (RelationContext relationContext = new RelationContext())
            {
                try
                {
                    MapInfo map = GetMapFromDb(version, folderName, mapContext);
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

        private static MapInfo GetMapFromDb(string version, string folderName, MapInfoContext mapContext)
        {
            var map = mapContext.MapInfos.FirstOrDefault(k =>
                k.Version == version && k.Folder == folderName);
            if (map == null)
            {
                mapContext.MapInfos.Add(new MapInfo(Guid.NewGuid().ToString(), version, folderName, 0, null));
                mapContext.SaveChanges();
                map = mapContext.MapInfos.First(k => k.Version == version && k.Folder == folderName);
            }

            return map;
        }

    }
}
