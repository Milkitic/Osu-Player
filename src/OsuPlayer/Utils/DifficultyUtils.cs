#nullable enable

using System.Windows.Media;

namespace Milki.OsuPlayer.Utils;

public static class DifficultyUtils
{
    private static LinearGradientBrush? _brush;

    public static Color GetColorByStarRating(double sr)
    {
        _brush ??= (LinearGradientBrush?)App.Current.FindResource("DifficultyBrush");
        if (_brush == null) throw new Exception("Can not find brush resource.");
        return _brush.GradientStops.GetRelativeColor(sr / 10);
    }
}