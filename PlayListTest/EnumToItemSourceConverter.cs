using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace PlayListTest
{
    public class EnumToItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var type = (Type)parameter;
                var list = Enum.GetValues(type).Cast<Enum>().ToList();
                return list;
            }
            catch (Exception ex)
            {
            }

            return Array.Empty<object>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}