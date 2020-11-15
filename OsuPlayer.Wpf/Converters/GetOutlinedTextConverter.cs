using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Milky.OsuPlayer.Converters
{
    class GetOutlinedTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                string result = null;
                var split = s.Split(' ');
                if (split.Length > 1)
                {
                    result = (split[0].Substring(0, 1) + split[1].Substring(0, 1)).ToUpper();
                }
                else if (split.Length > 0)
                {
                    if (split[0].Length > 1)
                    {
                        result = split[0].Substring(0, 2);
                    }
                    else
                        result = split[0].Substring(0, 1);
                }

                if (result == null) return null;
                if (result.All(k => k >= 32 && k <= 255))
                {
                    return result;
                }
                else
                {
                    return result.Substring(0, 1);
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}