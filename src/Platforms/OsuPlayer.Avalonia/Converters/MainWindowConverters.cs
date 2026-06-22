using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using OsuPlayer.Lang;

namespace OsuPlayer.Converters;

public class WindowMarginConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is WindowState state)
            return state == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
        return new Thickness(0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}

public class TitleIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isNavigationCollapsed)
        {
            try
            {
                var uri = new Uri(isNavigationCollapsed
                    ? "avares://OsuPlayer/Assets/title_sm.png"
                    : "avares://OsuPlayer/Assets/title.png");
                using var stream = AssetLoader.Open(uri);
                return new Bitmap(stream);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}

public class LyricWindowShownConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool shown)
        {
            var param = System.Convert.ToString(parameter);
            if (param == "string")
                return shown ? I18NUtil.GetString(SRKeys.Ui_CloseDesktopLyric) : I18NUtil.GetString(SRKeys.Ui_OpenDesktopLyric);
            if (param == "bool")
                return shown;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}

public class LyricWindowLockedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool locked)
            return locked ? I18NUtil.GetString(SRKeys.Ui_UnlockLyric) : I18NUtil.GetString(SRKeys.Ui_LockLyric);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}

public class BoolTrueToVisibleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool show)
            return show;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}

public class BoolFalseToVisibleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool show)
            return !show;
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}

public class EnumDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
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

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}
