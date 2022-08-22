using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

public class WindowMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var state = (WindowState)value;
        return state == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

//public class IconMarginConverter : IValueConverter
//{
//    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
//    {
//        if (value is bool isNavigationCollapsed)
//        {
//            return isNavigationCollapsed
//                ? new Thickness(13, 0, 0, 0)
//                : new Thickness(20, 0, 0, 0);
//        }

//        return new Thickness(0);
//    }

//    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
//    {
//        throw new NotImplementedException();
//    }
//}