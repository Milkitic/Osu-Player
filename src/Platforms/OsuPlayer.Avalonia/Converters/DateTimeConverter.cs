using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OsuPlayer.Avalonia.Converters;

public class DateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return ((DateTime?)value)?.ToString("g");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
