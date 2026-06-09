using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using OsuPlayer.Utils;

namespace OsuPlayer.Converters;

public class StarRating2ColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return DifficultyUtils.GetColorByStarRating(d);
        return DifficultyUtils.GetColorByStarRating(-1);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}
