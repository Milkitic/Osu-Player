using System.Globalization;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

public class NegativeBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var b = (bool)value;
        return !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}