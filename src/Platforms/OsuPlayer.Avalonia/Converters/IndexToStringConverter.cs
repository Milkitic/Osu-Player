using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace OsuPlayer.Converters;

class IndexToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
            return (index + 1).ToString("00");
        return "01";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}
