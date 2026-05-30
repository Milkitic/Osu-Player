using System;
using System.Collections.Generic;
using System.Linq;
using Milky.OsuPlayer.Shared;

namespace Milky.OsuPlayer.Core
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
                    var result = beatmapDataModel.Title.Contains(keyword, true) ||
                                 beatmapDataModel.TitleUnicode.Contains(keyword, true) ||
                                 beatmapDataModel.Artist.Contains(keyword, true) ||
                                 beatmapDataModel.ArtistUnicode.Contains(keyword, true) ||
                                 beatmapDataModel.SongTags.Contains(keyword, true) ||
                                 beatmapDataModel.SongSource.Contains(keyword, true) ||
                                 beatmapDataModel.Creator.Contains(keyword, true) ||
                                 beatmapDataModel.Version.Contains(keyword, true);
                    if (result)
                        resultList.Add(beatmapDataModel);
                }

            }

            return resultList;
        }
    }
}
