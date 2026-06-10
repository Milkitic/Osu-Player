using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Converters;

public class PlayingConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 1 && values[0] is bool isPlaying)
        {
            return isPlaying ? "⏸" : "▶";
        }
        return "▶";
    }
}

public class PlayModeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            PlaylistMode.Normal => "⇄",
            PlaylistMode.Random => "🔀",
            PlaylistMode.Loop => "↺",
            PlaylistMode.LoopRandom => "🔀↺",
            PlaylistMode.Single => "•",
            PlaylistMode.SingleLoop => "1",
            _ => "⇄"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}

public class BoolIsFavToSvgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? "♥" : "♡";
        return "♡";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}
