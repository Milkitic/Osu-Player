using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Milki.OsuPlayer.UiComponents.NotificationComponent.Converters;

public class NotificationTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (!(parameter is string str))
            {
                return Visibility.Visible;
            }

            var hidStyle = Visibility.Collapsed;

            var s = str.Split(';');
            if (s.Length > 1)
            {
                hidStyle = (Visibility)Enum.Parse(typeof(Visibility), s[1]);
            }

            var values = s[0].Split(',')
                .Select(k => (NotificationType)Enum.Parse(typeof(NotificationType), k))
                .ToArray();

            return value is NotificationType actualType && values.Contains(actualType)
                ? Visibility.Visible
                : hidStyle;
        }
        catch
        {
            return Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}