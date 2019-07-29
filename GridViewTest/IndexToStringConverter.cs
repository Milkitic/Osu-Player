using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GridViewTest
{
    public class IndexToStringConverter : IMultiValueConverter
    {
        public static readonly Dictionary<dynamic, int> Pairs = new Dictionary<dynamic, int>();
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length == 2)
                {
                    dynamic obj = values[0];
                    dynamic list = values[1];

                    return list.IndexOf(obj) + 1;
                }

                return 0;
            }
            catch (Exception exc)
            {
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}