using Milkitic.OsuPlayer.Wpf.Models;
using Milkitic.OsuPlayer.Wpf.Utils;
using osu.Shared;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Milkitic.OsuPlayer.Wpf.Data
{
    internal static class EntryQuery
    {
        public static IEnumerable<BeatmapEntry> GetListByTitleArtist(string title, string artist, IEnumerable<BeatmapEntry> list)
        {
            return list
                .Where(k => k.Title != null && k.Title == title ||
                            k.TitleUnicode != null && k.TitleUnicode == title)
                .Where(k => k.Artist != null && k.Artist == artist ||
                            k.ArtistUnicode != null && k.ArtistUnicode == artist);
        }

        public static IEnumerable<BeatmapEntry> GetListByKeyword(string keyword, IEnumerable<BeatmapEntry> list)
        {
            if (string.IsNullOrEmpty(keyword))
                return list;
            string[] keywords = keyword.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            return keywords.Aggregate(list,
                (current, keywd) => current.Where(k =>
                    k.Title != null && k.Title.ToLower().Contains(keywd.ToLower()) ||
                    k.TitleUnicode != null && k.TitleUnicode.Contains(keywd) ||
                    k.Artist != null && k.Artist.ToLower().Contains(keywd.ToLower()) ||
                    k.ArtistUnicode != null && k.ArtistUnicode.Contains(keywd) ||
                    k.SongTags != null && k.SongTags.ToLower().Contains(keywd.ToLower()) ||
                    k.SongSource != null && k.SongSource.ToLower().Contains(keywd.ToLower()) ||
                    k.Creator != null && k.Creator.ToLower().Contains(keywd.ToLower()) ||
                    k.Version != null && k.Version.ToLower().Contains(keywd.ToLower())
                ));
        }

        public static IEnumerable<BeatmapEntry> GetBeatmapsetsByFolder(this IEnumerable<BeatmapEntry> list,
            string folder)
        {
            return list.Where(k => k.FolderName == folder);
        }

        public static BeatmapEntry GetHighestDiff(this IEnumerable<BeatmapEntry> list)
        {
            var ok = list.GroupBy(k => k.GameMode).ToDictionary(k => k.Key, k => k);
            if (ok.ContainsKey(GameMode.Standard))
                return ok[GameMode.Standard].OrderBy(k => k.DiffStarRatingStandard[Mods.None]).Last();
            if (ok.ContainsKey(GameMode.Mania))
                return ok[GameMode.Mania].OrderBy(k => k.DiffStarRatingMania[Mods.None]).Last();
            if (ok.ContainsKey(GameMode.CatchTheBeat))
                return ok[GameMode.CatchTheBeat].OrderBy(k => k.DiffStarRatingCtB[Mods.None]).Last();
            return ok[GameMode.Taiko].OrderBy(k => k.DiffStarRatingTaiko[Mods.None]).Last();
        }
        public static IEnumerable<BeatmapEntry> GetRecentListFromDb(
            this IEnumerable<BeatmapEntry> list)
        {
            var recent = DbOperator.GetRecent().ToList();
            return list.GetMapListFromDb(recent);
        }

        public static IEnumerable<BeatmapEntry> GetMapListFromDb(
            this IEnumerable<BeatmapEntry> list, List<MapInfo> infos)
        {

            var db = new List<(BeatmapEntry entry, DateTime dateTime)>();
            foreach (BeatmapEntry k in list)
            {
                foreach (var mapInfo in infos)
                {
                    if (mapInfo.FolderName == k.FolderName && mapInfo.Version == k.Version)
                    {
                        db.Add((k, mapInfo.LastPlayTime ?? new DateTime()));
                        break;
                    }
                }
            }

            return db.OrderByDescending(k => k.dateTime).Select(k => k.entry);
        }

        public static IEnumerable<BeatmapEntry> SortBy(this IEnumerable<BeatmapEntry> list, SortMode sortMode)
        {
            switch (sortMode)
            {
                case SortMode.Artist:
                default:
                    return list.OrderBy(k => MetaSelect.GetUnicode(k.Artist, k.ArtistUnicode),
                        StringComparer.InvariantCulture);
                case SortMode.Title:
                    return list.OrderBy(k => MetaSelect.GetUnicode(k.Title, k.TitleUnicode),
                        StringComparer.InvariantCulture);
            }
        }

        public static IEnumerable<BeatmapViewModel> Transform(this IEnumerable<BeatmapEntry> list, bool multiVersions)
        {
            return list.Select((entry, i) => new BeatmapViewModel
            {
                Id = multiVersions ? (i + 1).ToString("00") : "",
                Artist = entry.Artist,
                ArtistUnicode = entry.ArtistUnicode,
                BeatmapId = entry.BeatmapId,
                Creator = entry.Creator,
                FolderName = entry.FolderName,
                GameMode = entry.GameMode,
                SongSource = entry.SongSource,
                SongTags = entry.SongTags,
                Title = entry.Title,
                TitleUnicode = entry.TitleUnicode,
                Version = entry.Version,
            }).Distinct(new Comparer(multiVersions)).ToList();
        }

        public class Comparer : IEqualityComparer<BeatmapViewModel>
        {
            private readonly bool _multiVersions;

            public Comparer(bool multiVersions)
            {
                _multiVersions = multiVersions;
            }

            public bool Equals(BeatmapViewModel x, BeatmapViewModel y)
            {
                if (x.AutoArtist != y.AutoArtist) return false;
                if (x.AutoTitleSource != y.AutoTitleSource) return false;
                if (x.Creator != y.Creator) return false;
                if (_multiVersions)
                {
                    if (x.Version != y.Version) return false;
                }

                return true;
            }

            public int GetHashCode(BeatmapViewModel obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
