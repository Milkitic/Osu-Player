using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace OsuPlayer.Converters;

public class MiniWindowConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isMini)
            return isMini;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}
