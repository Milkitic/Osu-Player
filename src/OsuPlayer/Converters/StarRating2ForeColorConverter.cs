using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Milky.OsuPlayer.Converters;

public class StarRating2ForeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double d) return Color.FromRgb(36, 36, 36);
        if (d < 6.7)
        {
            return Color.FromRgb(28, 23, 25);
        }

        return Color.FromRgb(255, 217, 102);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}