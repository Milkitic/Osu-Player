using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Converters;

public class PlayModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(value is PlaylistMode playerMode)) return value;
        switch (playerMode)
        {
            case PlaylistMode.Normal:
                return Application.Current.FindResource($"ModeNormal{parameter}Templ");
            case PlaylistMode.Random:
                return Application.Current.FindResource($"ModeRandom{parameter}Templ");
            case PlaylistMode.Loop:
                return Application.Current.FindResource($"ModeLoop{parameter}Templ");
            case PlaylistMode.LoopRandom:
                return Application.Current.FindResource($"ModeLoopRandom{parameter}Templ");
            case PlaylistMode.Single:
                return Application.Current.FindResource($"ModeSingle{parameter}Templ");
            case PlaylistMode.SingleLoop:
                return Application.Current.FindResource($"ModeSingleLoop{parameter}Templ");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}