using Milkitic.OsuPlayer;
using Milkitic.OsuPlayer.Utils;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer
{
    internal static class Query
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

        public static BeatmapEntry[] GetStringsBySortType(SortEnum sortEnum, IEnumerable<BeatmapEntry> list)
        {
            switch (sortEnum)
            {
                case SortEnum.Artist:
                default:
                    return list.Distinct().OrderBy(k => MetaSelect.GetUnicode(k.Artist, k.ArtistUnicode),
                        StringComparer.InvariantCulture).ToArray();
                case SortEnum.Title:
                    return list.Distinct().OrderBy(k => MetaSelect.GetUnicode(k.Title, k.TitleUnicode),
                        StringComparer.InvariantCulture).ToArray();
            }
        }
    }
}
