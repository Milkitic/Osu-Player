using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Metadata;
using OSharp.Beatmap;
using OSharp.Common;
using osu.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OSharp.Beatmap.MetaData;
using GameMode = OSharp.Beatmap.Sections.GamePlay.GameMode;

namespace Milky.OsuPlayer.Common.Data
{
    public static class BeatmapDatabaseQuery
    {
#if false
        class BeatmapCachedInfo
        {
            public BeatmapCachedInfo(Beatmap beatmap)
            {
                Beatmap = beatmap;
            }

            public DateTime Time { get; set; } = DateTime.Now;
            public Beatmap Beatmap { get; }

            public override int GetHashCode()
            {
                return Beatmap.GetHashCode();
            }

            public static BeatmapCachedInfo FromBeatmap(Beatmap beatmap)
            {
                return new BeatmapCachedInfo(beatmap);
            }
        }

        private static readonly ConcurrentRandom Random = new ConcurrentRandom();

        private static readonly HashSet<BeatmapCachedInfo> CachedBeatmaps = new HashSet<BeatmapCachedInfo>();

        public static List<Beatmap> FilterByTitleArtist(string title, string artist)
        {
            using (var context = new BeatmapDbContext())
            {
                var result = context.Beatmaps
                    .Where(k => k.Title != null && k.Title == title ||
                                k.TitleUnicode != null && k.TitleUnicode == title)
                    .Where(k => k.Artist != null && k.Artist == artist ||
                                k.ArtistUnicode != null && k.ArtistUnicode == artist).ToList();
                StoreCache(result);
                return result;
            }
        }

        public static List<Beatmap> FilterByKeyword(string keywordStr)
        {
            using (var context = new BeatmapDbContext())
            {
                if (string.IsNullOrWhiteSpace(keywordStr))
                    return context.Beatmaps.ToList();
                string[] keywords = keywordStr.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                return keywords.Aggregate<string, IEnumerable<Beatmap>>(context.Beatmaps,
                    (current, keyword) => current.Where(k =>
                        k.Title?.Contains(keyword, true) == true ||
                        k.TitleUnicode?.Contains(keyword, true) == true ||
                        k.Artist?.Contains(keyword, true) == true ||
                        k.ArtistUnicode?.Contains(keyword, true) == true ||
                        k.SongTags?.Contains(keyword, true) == true ||
                        k.SongSource?.Contains(keyword, true) == true ||
                        k.Creator?.Contains(keyword, true) == true ||
                        k.Version?.Contains(keyword, true) == true
                    )).ToList();
            }
        }

        public static List<Beatmap> FilterByFolder(string folder)
        {
            using (var context = new BeatmapDbContext())
            {
                return context.Beatmaps.Where(k => k.FolderName == folder).ToList();
            }
        }

        public static Beatmap FilterByIdentity(MapIdentity identity)
        {
            using (var context = new BeatmapDbContext())
            {
                return context.Beatmaps.Where(k => k != null).FirstOrDefault(k => k.FolderName == identity.FolderName && k.Version == identity.Version);
            }
        }

        public static List<Beatmap> FilterByIdentities(IEnumerable<MapIdentity> identities)
        {
            using (var context = new BeatmapDbContext())
            {
                return identities.Select(id => context.Beatmaps.FirstOrDefault(k => k.FolderName == id.FolderName && k.Version == id.Version)).ToList(); //todo: need optimize
            }
        }

        public static Beatmap GetHighestDiff(IEnumerable<Beatmap> enumerable)
        {
            var dictionary = enumerable.GroupBy(k => k.GameMode).ToDictionary(k => k.Key, k => k.ToList());
            if (dictionary.ContainsKey(GameMode.Circle))
            {
                return dictionary[GameMode.Circle].Aggregate((i1, i2) => i1.DiffSrNoneStandard > i2.DiffSrNoneStandard ? i1 : i2);
            }
            if (dictionary.ContainsKey(GameMode.Mania))
            {
                return dictionary[GameMode.Mania].Aggregate((i1, i2) => i1.DiffSrNoneMania > i2.DiffSrNoneMania ? i1 : i2);
            }

            if (dictionary.ContainsKey(GameMode.Catch))
            {
                return dictionary[GameMode.Catch].Aggregate((i1, i2) => i1.DiffSrNoneCtB > i2.DiffSrNoneCtB ? i1 : i2);
            }

            if (dictionary.ContainsKey(GameMode.Taiko))
            {
                return dictionary[GameMode.Taiko].Aggregate((i1, i2) => i1.DiffSrNoneTaiko > i2.DiffSrNoneTaiko ? i1 : i2);
            }

            Console.WriteLine(@"Get highest difficulty failed.");
            var randKey = dictionary.Keys.ToList()[Random.Next(dictionary.Keys.Count)];
            return dictionary[randKey][dictionary[randKey].Count];
            //enumerable.ToList()[Random.Next(enumerable.Count())];
        }

        public static IEnumerable<Beatmap> GetRecentListFromDb()
        {
            using (var context = new BeatmapDbContext())
            {
                throw new NotImplementedException();
            }
        }

        public static IEnumerable<Beatmap> SortBy(IEnumerable<Beatmap> list, SortMode sortMode)
        {
            switch (sortMode)
            {
                case SortMode.Artist:
                default:
                    return list.OrderBy(k => MetaString.GetUnicode(k.Artist, k.ArtistUnicode),
                        StringComparer.InvariantCulture);
                case SortMode.Title:
                    return list.OrderBy(k => MetaString.GetUnicode(k.Title, k.TitleUnicode),
                        StringComparer.InvariantCulture);
            }
        }

        public static List<Beatmap> GetMaps(List<MapInfo> maps, bool playedOrAddedTime = true)
        {
            throw new NotImplementedException();
        }
        public static IEnumerable<Beatmap> GetBeatmapsByMapInfo(IEnumerable<IMapIdentifiable> enumerable, bool playedOrAddedTime = true)
        {
            if (enumerable is IEnumerable<Beatmap> foo)
                return foo;

            throw new NotImplementedException();
        }

        private static void StoreCache(IEnumerable<Beatmap> result)
        {
            CachedBeatmaps.UnionWith(result.Select(BeatmapCachedInfo.FromBeatmap));
        }
#endif

        public static async Task<List<Beatmap>> GetWholeListFromDbAsync()
        {
            return await Task.Run(() =>
            {
                using (var context = new BeatmapDbContext())
                {
                    return context.Beatmaps.ToList();
                }
            });
        }
    }
}
