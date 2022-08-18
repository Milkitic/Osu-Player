using System.Globalization;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters
{
    public class RoundedNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var d = System.Convert.ToDouble(value);
            return Math.Round(d, 3);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var d = System.Convert.ToDouble(value);
            return d;
        }
    }
}