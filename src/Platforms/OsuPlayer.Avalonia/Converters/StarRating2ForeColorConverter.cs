using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace OsuPlayer.Converters;

/// <summary>
/// StarRating → 前景色(Numeric/Color 的星等级颜色)。
/// </summary>
public class StarRating2ForeColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return new SolidColorBrush(Color.FromRgb(255, 255, 255));
        try
        {
            var d = System.Convert.ToDouble(value);
            if (d < 2) return new SolidColorBrush(Color.FromRgb(120, 200, 120));
            if (d < 4) return new SolidColorBrush(Color.FromRgb(220, 220, 100));
            if (d < 6) return new SolidColorBrush(Color.FromRgb(220, 130, 80));
            return new SolidColorBrush(Color.FromRgb(220, 80, 80));
        }
        catch
        {
            return new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
