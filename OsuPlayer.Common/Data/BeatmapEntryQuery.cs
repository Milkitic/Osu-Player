using System;
using System.Collections.Generic;
using System.Linq;
using Milky.OsuPlayer.Common.Metadata;
using osu.Shared;
using osu_database_reader.Components.Beatmaps;
using OSharp.Beatmap;
using OSharp.Common;

namespace Milky.OsuPlayer.Common.Data
{
    public static class BeatmapEntryQuery
    {
        //public static OsuDb BeatmapDb { get; set; }
        //public static List<BeatmapEntry> Beatmaps => BeatmapDb?.Beatmaps;

        private static readonly ConcurrentRandom Random = new ConcurrentRandom();

        public static IEnumerable<BeatmapEntry> FilterByTitleArtist(this IEnumerable<BeatmapEntry> list, string title,
            string artist)
        {
            return list
                .Where(k => k.Title != null && k.Title == title ||
                            k.TitleUnicode != null && k.TitleUnicode == title)
                .Where(k => k.Artist != null && k.Artist == artist ||
                            k.ArtistUnicode != null && k.ArtistUnicode == artist);
        }

        public static IEnumerable<BeatmapEntry> FilterByKeyword(this IEnumerable<BeatmapEntry> list, string keywordStr)
        {
            if (string.IsNullOrWhiteSpace(keywordStr))
                return list;
            string[] keywords = keywordStr.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            return keywords.Aggregate(list,
                (current, keywd) => current.Where(k =>
                    k.Title?.Contains(keywd, true) == true ||
                    k.TitleUnicode?.Contains(keywd, true) == true ||
                    k.Artist?.Contains(keywd, true) == true ||
                    k.ArtistUnicode?.Contains(keywd, true) == true ||
                    k.SongTags?.Contains(keywd, true) == true ||
                    k.SongSource?.Contains(keywd, true) == true ||
                    k.Creator?.Contains(keywd, true) == true ||
                    k.Version?.Contains(keywd, true) == true
                ));
        }

        public static IEnumerable<BeatmapEntry> FilterByFolder(this IEnumerable<BeatmapEntry> list,
            string folder)
        {
            return list.Where(k => k.FolderName == folder);
        }

        public static BeatmapEntry FilterByIdentity(this IEnumerable<BeatmapEntry> list,
            MapIdentity identity)
        {
            return list.Where(k => k != null).FirstOrDefault(k => k.FolderName == identity.FolderName && k.Version == identity.Version);
        }

        public static IEnumerable<BeatmapEntry> FilterByIdentities(this IEnumerable<BeatmapEntry> list,
            IEnumerable<MapIdentity> identities)
        {
            return identities.Select(id => list.FirstOrDefault(k => k.FolderName == id.FolderName && k.Version == id.Version));
        }

        public static BeatmapEntry GetHighestDiff(this IEnumerable<BeatmapEntry> enumerable)
        {
            var dictionary = enumerable.GroupBy(k => k.GameMode).ToDictionary(k => k.Key, k => k.ToList());
            if (dictionary.ContainsKey(GameMode.Standard))
            {
                if (dictionary[GameMode.Standard].All(k => k.DiffStarRatingStandard.ContainsKey(Mods.None)))
                    return dictionary[GameMode.Standard].OrderBy(k => k.DiffStarRatingStandard[Mods.None]).Last();
            }
            if (dictionary.ContainsKey(GameMode.Mania))
            {
                if (dictionary[GameMode.Mania].All(k => k.DiffStarRatingMania.ContainsKey(Mods.None)))
                    return dictionary[GameMode.Mania].OrderBy(k => k.DiffStarRatingMania[Mods.None]).Last();
            }

            if (dictionary.ContainsKey(GameMode.CatchTheBeat))
            {
                if (dictionary[GameMode.CatchTheBeat].All(k => k.DiffStarRatingCtB.ContainsKey(Mods.None)))
                    return dictionary[GameMode.CatchTheBeat].OrderBy(k => k.DiffStarRatingCtB[Mods.None]).Last();
            }

            if (dictionary.ContainsKey(GameMode.Taiko))
            {
                if (dictionary[GameMode.Taiko].All(k => k.DiffStarRatingTaiko.ContainsKey(Mods.None)))
                    return dictionary[GameMode.Taiko].OrderBy(k => k.DiffStarRatingTaiko[Mods.None]).Last();
            }

            Console.WriteLine(@"Get highest difficulty failed.");
            var randKey = dictionary.Keys.ToList()[Random.Next(dictionary.Keys.Count)];
            return dictionary[randKey][dictionary[randKey].Count];
            //enumerable.ToList()[Random.Next(enumerable.Count())];
        }

        public static IEnumerable<BeatmapEntry> GetRecentListFromDb(
            this IEnumerable<BeatmapEntry> list)
        {
            var recent = DbOperate.GetRecent().ToList();
            return recent.ToBeatmapEntries(list);
        }

        public static IEnumerable<BeatmapEntry> SortBy(this IEnumerable<BeatmapEntry> list, SortMode sortMode)
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

    
    }
}
