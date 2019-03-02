using System;
using System.Globalization;
using System.Windows.Data;

namespace Milky.OsuPlayer.Converter
{
    class IndexToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var index = (int)value;
            return (index + 1).ToString("00");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
