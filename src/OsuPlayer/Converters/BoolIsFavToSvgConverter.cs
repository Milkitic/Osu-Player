using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

public class BoolIsFavToSvgConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(value is bool b)) return Application.Current.FindResource("HeartDisabledTempl");
        return b
            ? Application.Current.FindResource("HeartEnabledTempl")
            : Application.Current.FindResource("HeartDisabledTempl");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}