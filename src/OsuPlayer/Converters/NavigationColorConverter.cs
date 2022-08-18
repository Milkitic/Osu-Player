using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Milki.OsuPlayer.Converters
{
    public class NavigationColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            var brush = (SolidColorBrush)value;
            if (brush.Color == Color.FromRgb(0, 0, 0))
                return new SolidColorBrush(Color.FromRgb(220, 73, 141));
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
