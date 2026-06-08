using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using OsuPlayer.Utils;

namespace OsuPlayer.Converters;

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

public class TitleIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isNavigationCollapsed)
        {
            return !isNavigationCollapsed
                ? App.Current.FindResource("TitleLogo")
                : App.Current.FindResource("TitleLogoSmall");
        }

        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class LyricWindowShownConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var shown = (bool)value;
        if (System.Convert.ToString(parameter) == "string")
        {
            return shown ? I18NUtil.GetString("ui-closeDesktopLyric") : I18NUtil.GetString("ui-openDesktopLyric");
        }
        else if (System.Convert.ToString(parameter) == "bool")
        {
            return shown;
        }
        else return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class LyricWindowLockedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var locked = (bool)value;
        return locked ? I18NUtil.GetString("ui-unlockLyric") : I18NUtil.GetString("ui-lockLyric");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolTrueToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var show = (bool)value;
        return show ? Visibility.Visible : Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolFalseToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var show = (bool)value;
        return !show ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name == null) return value.ToString();
        var field = type.GetField(name);
        if (field == null) return value.ToString();
        var attr = (System.ComponentModel.DescriptionAttribute?)System.Attribute.GetCustomAttribute(field, typeof(System.ComponentModel.DescriptionAttribute));
        return attr?.Description ?? value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
