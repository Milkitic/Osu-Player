using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Converters;

public class PlayModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(value is PlayListMode playerMode)) return value;
        switch (playerMode)
        {
            case PlayListMode.Normal:
                return Application.Current.FindResource($"ModeNormal{parameter}Templ");
            case PlayListMode.Random:
                return Application.Current.FindResource($"ModeRandom{parameter}Templ");
            case PlayListMode.Loop:
                return Application.Current.FindResource($"ModeLoop{parameter}Templ");
            case PlayListMode.LoopRandom:
                return Application.Current.FindResource($"ModeLoopRandom{parameter}Templ");
            case PlayListMode.Single:
                return Application.Current.FindResource($"ModeSingle{parameter}Templ");
            case PlayListMode.SingleLoop:
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