using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

public class Multi_EqualityToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 0)
        {
            var first = values[0];

            foreach (var val in values.Skip(1))
            {
                if (val != first)
                    return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}