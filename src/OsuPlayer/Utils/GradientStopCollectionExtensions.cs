using System.Collections.Concurrent;
using System.Windows.Media;

namespace Milki.OsuPlayer.Utils;

public static class GradientStopCollectionExtensions
{
    private static readonly ConcurrentDictionary<GradientStopCollection, GradientStopCollectionInfo> Cache = new();
    public static Color GetRelativeColor(this GradientStopCollection gsc, double offset)
    {
        var info = Cache.GetOrAdd(gsc, collection =>
        {
            var gradientStops = collection.OrderBy(k => k.Offset).ToArray();
            var gradientStopCollectionInfo = new GradientStopCollectionInfo
            {
                Anchors = gradientStops
                    .DistinctBy(k => k.Offset)
                    .ToDictionary(k => k.Offset, k => k.Color),
                FirstGradientStop = gradientStops[0],
                LastGradientStop = gradientStops[^1]
            };
            return gradientStopCollectionInfo;
        });

        if (info.Anchors.TryGetValue(offset, out var anchorColor))
        {
            return anchorColor;
        }

        var before = info.FirstGradientStop;
        var after = info.LastGradientStop;
        foreach (var gs in gsc)
        {
            if (gs.Offset < offset && gs.Offset > before.Offset)
            {
                before = gs;
            }

            if (gs.Offset > offset && gs.Offset < after.Offset)
            {
                after = gs;
            }
        }

        var color = new Color
        {
            ScA = (float)((offset - before.Offset) * (after.Color.ScA - before.Color.ScA) /
                (after.Offset - before.Offset) + before.Color.ScA),
            ScR = (float)((offset - before.Offset) * (after.Color.ScR - before.Color.ScR) /
                (after.Offset - before.Offset) + before.Color.ScR),
            ScG = (float)((offset - before.Offset) * (after.Color.ScG - before.Color.ScG) /
                (after.Offset - before.Offset) + before.Color.ScG),
            ScB = (float)((offset - before.Offset) * (after.Color.ScB - before.Color.ScB) /
                (after.Offset - before.Offset) + before.Color.ScB)
        };

        return color;
    }
}

internal class GradientStopCollectionInfo
{
    public Dictionary<double, Color> Anchors { get; set; }
    public GradientStop FirstGradientStop { get; set; }
    public GradientStop LastGradientStop { get; set; }
}