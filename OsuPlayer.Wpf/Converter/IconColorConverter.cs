using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Milky.OsuPlayer.Converter
{
    public class IconColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2)
            {
                var b1 = values[0] as Brush;
                var b2 = values[1] as Brush;
                if (b2 != null)
                {
                    return b2;
                }

                if (b1 != null)
                {
                    return b1;
                }
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}