using System.Globalization;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

public class NullToFullOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is null ? 1 : 0.15;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}