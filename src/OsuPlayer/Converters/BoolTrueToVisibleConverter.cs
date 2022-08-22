using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

public class BoolTrueToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var show = (bool)value;
        if (parameter != null)
            return show ? Visibility.Visible : Visibility.Collapsed;
        return show ? Visibility.Visible : Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}