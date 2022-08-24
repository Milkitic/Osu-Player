using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Milki.OsuPlayer.UiComponents.NotificationComponent.Converters;

public class EmptyToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            return string.IsNullOrEmpty(s)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}