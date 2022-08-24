using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Milki.OsuPlayer.Converters;

public class StarRating2ForeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        //var color = value is double d
        //    ? DifficultyUtils.GetColorByStarRating(d)
        //    : DifficultyUtils.GetColorByStarRating(-1);
        //var rgb = new RGB(color.R, color.G, color.B);
        //var hsl = ColorConverter.RgbToHsl(rgb);

        //if (hsl.L >= 57)
        //{
        //    return Color.FromRgb(36, 36, 36);
        //}

        //return Color.FromRgb(253, 253, 253);
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