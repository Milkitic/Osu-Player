using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Metadata;
using OSharp.Beatmap;
using OSharp.Beatmap.MetaData;
using OSharp.Beatmap.Sections.GamePlay;
using OSharp.Common;

namespace Milky.OsuPlayer.Common.Data
{
    public static class BeatmapQuery
    {
        private static readonly ConcurrentRandom Random = new ConcurrentRandom();
        private static HashSet<Beatmap> Beatmaps => InstanceManage.GetInstance<OsuDbInst>().Beatmaps;

        public static List<Beatmap> FilterByTitleArtist(string title, string artist)
        {
            var result = Beatmaps
                .Where(k => k.Title != null && k.Title == title ||
                            k.TitleUnicode != null && k.TitleUnicode == title)
                .Where(k => k.Artist != null && k.Artist == artist ||
                            k.ArtistUnicode != null && k.ArtistUnicode == artist).ToList();
            return result;
        }

        public static List<Beatmap> FilterByKeyword(string keywordStr)
        {
            if (string.IsNullOrWhiteSpace(keywordStr))
                return Beatmaps.ToList();
            string[] keywords = keywordStr.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            return keywords.Aggregate<string, IEnumerable<Beatmap>>(Beatmaps,
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

        public static List<Beatmap> FilterByFolder(string folder)
        {
            return Beatmaps.Where(k => k.FolderName == folder).ToList();
        }

        public static Beatmap FilterByIdentity(MapIdentity identity)
        {
            return Beatmaps.Where(k => k != null).FirstOrDefault(k => k.FolderName == identity.FolderName && k.Version == identity.Version);
        }

        public static List<Beatmap> FilterByIdentities(IEnumerable<MapIdentity> identities)
        {
            return identities.Select(id => Beatmaps.FirstOrDefault(k => k.FolderName == id.FolderName && k.Version == id.Version)).ToList();
        }

        public static Beatmap GetHighestDiff(this IEnumerable<Beatmap> enumerable)
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
            var recent = DbOperate.GetRecent().ToList();
            return GetBeatmapsByIdentifiable(recent);
        }

        public static IEnumerable<Beatmap> SortBy(this IEnumerable<Beatmap> list, SortMode sortMode)
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

        //public static IEnumerable<T> MaxBy<T,TOut>(this IEnumerable<T> enumerable, Func<T,TOut> selector) where T : IComparable
        //{
        //    return enumerable.Aggregate((i1, i2) => selector.Invoke() > i2.ID ? i1 : i2);
        //}
        public static List<Beatmap> GetWholeList()
        {
            return Beatmaps.ToList();
        }

        public static IEnumerable<Beatmap> GetBeatmapsByIdentifiable(IEnumerable<IMapIdentifiable> enumerable,
            bool playedOrAddedTime = true)
        {
            if (enumerable is IEnumerable<Beatmap> foo)
                return foo;

            var identifiableListCopy =
                enumerable.ToList(); /*enumerable is List<IMapIdentifiable> list ? list : enumerable.ToList();*/
            bool flag = true;

            var db = new List<(Beatmap entry, DateTime? lastPlayedTime, DateTime? addTime)>();
            Beatmaps.Where(k => enumerable.Any(inner => inner.FolderName == k.FolderName));
            foreach (Beatmap k in Beatmaps)
            {
                foreach (var identifiable in identifiableListCopy)
                {
                    if (identifiable.FolderName != k.FolderName || identifiable.Version != k.Version)
                        continue;

                    if (identifiable is MapInfo mapInfo)
                        db.Add((k, mapInfo.LastPlayTime ?? new DateTime(), mapInfo.AddTime));
                    else
                    {
                        db.Add((k, null, null));
                        flag = false;
                    }

                    identifiableListCopy.Remove(identifiable);
                    break;
                }
            }

            if (flag)
            {
                return playedOrAddedTime
                    ? db.OrderByDescending(k => k.lastPlayedTime).Select(k => k.entry)
                    : db.OrderByDescending(k => k.addTime).Select(k => k.entry);
            }

            return db.Select(k => k.entry);
        }
    }
}
