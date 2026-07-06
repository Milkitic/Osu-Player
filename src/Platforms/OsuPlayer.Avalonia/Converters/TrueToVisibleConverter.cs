using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OsuPlayer.Converters;

public class TrueToVisibleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var collapse = false;
        if (parameter is string s)
        {
            if (bool.TryParse(s, out var col))
            {
                collapse = col;
            }
        }

        var b = (bool?)value;
        if (b == true) return true;
        return !collapse;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
