using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Milky.OsuPlayer.Converters
{
    class TrueToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var collapse = false;
            if (parameter is string s)
            {
                if (bool.TryParse(s, out var col))
                {
                    collapse = col;
                }
            }

            var b = (bool?)value;
            if (b == true)
                return Visibility.Visible;
            return collapse ? Visibility.Collapsed : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}