using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OsuPlayer.Converters;

public class RoundedNumberConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return null;
        try
        {
            var d = System.Convert.ToDouble(value);
            return Math.Round(d, 3);
        }
        catch
        {
            return value;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return null;
        try
        {
            return System.Convert.ToDouble(value);
        }
        catch
        {
            return value;
        }
    }
}
