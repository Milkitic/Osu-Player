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
            var key = isPlaying ? "PauseTempl" : "PlayTempl";
            if (Application.Current != null && Application.Current.TryGetResource(key, null, out var resource))
            {
                return resource;
            }
        }
        return null;
    }
}

public class PlayModeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PlaylistMode playerMode)
        {
            var paramStr = parameter as string ?? string.Empty;
            var key = playerMode switch
            {
                PlaylistMode.Normal => $"ModeNormal{paramStr}Templ",
                PlaylistMode.Random => $"ModeRandom{paramStr}Templ",
                PlaylistMode.Loop => $"ModeLoop{paramStr}Templ",
                PlaylistMode.LoopRandom => $"ModeLoopRandom{paramStr}Templ",
                PlaylistMode.Single => $"ModeSingle{paramStr}Templ",
                PlaylistMode.SingleLoop => $"ModeSingleLoop{paramStr}Templ",
                _ => $"ModeNormal{paramStr}Templ"
            };

            if (Application.Current != null && Application.Current.TryGetResource(key, null, out var resource))
            {
                return resource;
            }
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}

public class BoolIsFavToSvgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = (value is bool b && b) ? "HeartEnabledTempl" : "HeartDisabledTempl";
        if (Application.Current != null && Application.Current.TryGetResource(key, null, out var resource))
        {
            return resource;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}
