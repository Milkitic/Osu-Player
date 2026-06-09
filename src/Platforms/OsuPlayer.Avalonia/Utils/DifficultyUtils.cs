using System;
using Avalonia.Media;

namespace OsuPlayer.Utils;

public static class DifficultyUtils
{
    public static Color GetColorByStarRating(double sr)
    {
        return sr switch
        {
            < 0 => Color.FromRgb(255, 255, 255),
            < 2 => Color.FromRgb(102, 204, 170),
            < 2.7 => Color.FromRgb(153, 204, 102),
            < 4 => Color.FromRgb(204, 204, 102),
            < 5.3 => Color.FromRgb(238, 170, 0),
            < 6.7 => Color.FromRgb(238, 136, 102),
            < 8 => Color.FromRgb(204, 102, 153),
            < 9.5 => Color.FromRgb(170, 68, 204),
            < 10.5 => Color.FromRgb(136, 102, 238),
            _ => Color.FromRgb(255, 255, 255)
        };
    }
}