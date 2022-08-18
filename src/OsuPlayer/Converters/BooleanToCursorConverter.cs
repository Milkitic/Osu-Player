using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace Milki.OsuPlayer.Converters
{
    class BooleanToCursorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var b = (bool?)value;
            if (b == true)
                return Cursors.Arrow;
            return Cursors.Hand;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}