using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Coosu.Beatmap.Sections.GamePlay;

namespace OsuPlayer.Converters;

public class GameModeToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not GameMode mode)
        {
            mode = GameMode.Circle;
        }

        var name = mode switch
        {
            GameMode.Circle => "circle",
            GameMode.Taiko => "taiko",
            GameMode.Catch => "fruit",
            GameMode.Mania => "mania",
            _ => "circle"
        };

        try
        {
            var uri = $"avares://OsuPlayer/Assets/mode_{name}.png";
            return new Bitmap(uri);
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
