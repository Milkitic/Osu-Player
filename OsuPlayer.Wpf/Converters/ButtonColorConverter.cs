using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Milki.OsuPlayer.Converters
{
    internal class ButtonColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var b = (bool?)value;
            if (b == true)
                return new SolidColorBrush(Color.FromArgb(255, 213, 213, 213));
            return new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
