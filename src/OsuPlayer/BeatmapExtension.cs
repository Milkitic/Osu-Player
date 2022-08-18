using Anotar.NLog;
using Coosu.Beatmap.Sections.GamePlay;
using Milki.OsuPlayer.Data.Models;

namespace Milki.OsuPlayer;

public static class BeatmapExtension
{
    public static PlayItem GetHighestDiff(this IEnumerable<PlayItem> enumerable)
    {
        var random = new Random(DateTime.Now.Ticks.GetHashCode());
        var dictionary = enumerable
            .GroupBy(k => k.PlayItemDetail.GameMode)
            .ToDictionary(k => k.Key, k => k.ToList());
        if (dictionary.TryGetValue(GameMode.Circle, out var value))
        {
            return value.Aggregate((item1, item2) =>
                item1.PlayItemDetail.DefaultStarRatingStd > item2.PlayItemDetail.DefaultStarRatingStd ? item1 : item2);
        }

        if (dictionary.TryGetValue(GameMode.Mania, out value))
        {
            return value.Aggregate((item1, item2) =>
                item1.PlayItemDetail.DefaultStarRatingMania > item2.PlayItemDetail.DefaultStarRatingMania ? item1 : item2);
        }

        if (dictionary.TryGetValue(GameMode.Catch, out value))
        {
            return value.Aggregate((item1, item2) =>
                item1.PlayItemDetail.DefaultStarRatingCtB > item2.PlayItemDetail.DefaultStarRatingCtB ? item1 : item2);
        }

        if (dictionary.TryGetValue(GameMode.Taiko, out value))
        {
            return value.Aggregate((item1, item2) =>
                item1.PlayItemDetail.DefaultStarRatingTaiko > item2.PlayItemDetail.DefaultStarRatingTaiko ? item1 : item2);
        }

        LogTo.Warn(@"Get highest difficulty failed.");
        var randKey = dictionary.Keys.ToList()[random.Next(dictionary.Keys.Count)];
        return dictionary[randKey][dictionary[randKey].Count];
    }
}