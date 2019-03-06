using System;
using System.Globalization;
using System.Windows.Data;

namespace Milky.OsuPlayer.Converter
{
    public class MsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                value = 0;
            return TimeSpan.FromMilliseconds((long)value).ToString(@"mm\:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}