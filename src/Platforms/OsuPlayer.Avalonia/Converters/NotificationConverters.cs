using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using OsuPlayer.Shared.Models;
using OsuPlayer.UiComponents.NotificationComponent;

namespace OsuPlayer.Converters;

internal class EmptyToVisibleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
            return !string.IsNullOrEmpty(s);
        return value is not null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}

internal class FontColorConverter : IValueConverter
{
    private static readonly Dictionary<Color, IBrush> s_brushes = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ISolidColorBrush b)
        {
            var c = b.Color;
            var newC = (c.R + c.G + c.B) / 3f > 128
                ? Color.FromArgb(255, 32, 32, 32)
                : Color.FromArgb(255, 240, 240, 240);
            if (!s_brushes.ContainsKey(newC))
                s_brushes.Add(newC, new SolidColorBrush(newC));
            return s_brushes[newC];
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}

internal class MixColorConverter : IValueConverter
{
    private static readonly Dictionary<Color, IBrush> s_brushes = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ISolidColorBrush b)
        {
            var c = b.Color;
            var newC = (c.R + c.G + c.B) / 3f > 128
                ? Color.FromArgb(c.A, Darker(c.R), Darker(c.G), Darker(c.B))
                : Color.FromArgb(c.A, Lighter(c.R), Lighter(c.G), Lighter(c.B));
            if (!s_brushes.ContainsKey(newC))
                s_brushes.Add(newC, new SolidColorBrush(newC));
            return s_brushes[newC];
        }
        return value;
    }

    private static byte Darker(byte b, double ratio = 0.15)
    {
        var newVal = b - b * ratio;
        return newVal < 0 ? (byte)0 : (byte)newVal;
    }

    private static byte Lighter(byte b, double ratio = 0.15)
    {
        var newVal = b + b * ratio;
        return newVal > 255 ? (byte)255 : (byte)newVal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}

internal class NotificationTypeConverter : IValueConverter
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            if (parameter is not string str)
                return true;

            var hidStyle = false;
            var s = str.Split(';');
            if (s.Length > 1)
                hidStyle = true;

            var values = s[0].Split(',')
                .Select(k => (NotificationOption.NotificationLevel)Enum.Parse(typeof(NotificationOption.NotificationLevel), k))
                .ToArray();

            return value is NotificationOption.NotificationLevel actualType && values.Contains(actualType)
                ? true
                : hidStyle;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return true;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}

internal class NotificationTypeToCursorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is NotificationOption.NotificationLevel actualType && actualType == NotificationOption.NotificationLevel.Alert
            ? StandardCursorType.Hand
            : StandardCursorType.Arrow;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}
