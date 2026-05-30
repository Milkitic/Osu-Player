using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Milky.OsuPlayer.Common
{
    public static class BeatmapDataModelExt
    {
        public static List<BeatmapDataModel> GetByKeyword(this IEnumerable<BeatmapDataModel> beatmaps, string keywordStr)
        {
            if (string.IsNullOrWhiteSpace(keywordStr))
            {
                if (beatmaps is List<BeatmapDataModel> list)
                    return list;
                return beatmaps.ToList();
            }

            var keywords = keywordStr.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            var resultList = new List<BeatmapDataModel>();
            foreach (var keyword in keywords)
            {
                foreach (var beatmapDataModel in beatmaps)
                {
                    var result = InsensitiveCaseContains(beatmapDataModel.Title, keyword) ||
                                 InsensitiveCaseContains(beatmapDataModel.TitleUnicode, keyword) ||
                                 InsensitiveCaseContains(beatmapDataModel.Artist, keyword) ||
                                 InsensitiveCaseContains(beatmapDataModel.ArtistUnicode, keyword) ||
                                 InsensitiveCaseContains(beatmapDataModel.SongTags, keyword) ||
                                 InsensitiveCaseContains(beatmapDataModel.SongSource, keyword) ||
                                 InsensitiveCaseContains(beatmapDataModel.Creator, keyword) ||
                                 InsensitiveCaseContains(beatmapDataModel.Version, keyword);
                    if (result)
                        resultList.Add(beatmapDataModel);
                }

            }

            return resultList;
        }

        private static bool InsensitiveCaseContains(string paragraph, string word)
        {
            if (paragraph == null) return false;
            return CultureInfo.CurrentCulture.CompareInfo.IndexOf(paragraph, word, CompareOptions.IgnoreCase) >= 0;
        }
    }
}