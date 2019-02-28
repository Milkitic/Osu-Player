using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Milky.OsuPlayer.Converter
{
    class TrueToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var b = (bool?)value;
            if (b == true)
                return Visibility.Visible;
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}