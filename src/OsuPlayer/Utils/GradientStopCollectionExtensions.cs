using System.Windows.Media;

namespace Milki.OsuPlayer.Utils;

public static class GradientStopCollectionExtensions
{
    public static Color GetRelativeColor(this GradientStopCollection gsc, double offset)
    {
        var point = gsc.SingleOrDefault(f => f.Offset.Equals(offset));
        if (point != null) return point.Color;

        var before = gsc.First(w => w.Offset.Equals(gsc.Min(m => m.Offset)));
        var after = gsc.First(w => w.Offset.Equals(gsc.Max(m => m.Offset)));

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