using Milky.OsuPlayer.ControlLibrary.Custom;
using Milky.OsuPlayer.Windows;
using Milky.WpfApi;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Milky.OsuPlayer.Common.Player;

namespace Milky.OsuPlayer.Converter
{
    public class PlayingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is bool isPlaying)
            {
                if (values[1] is Window window)
                {
                    if (window is LyricWindow lyricWindow)
                    {
                        return lyricWindow.FindResource(isPlaying ? "PauseButton" : "PlayButton");
                    }
                    if (window is MainWindow mainWindow)
                    {
                        return Application.Current.FindResource(isPlaying ? "PauseButtonStyle" : "PlayButtonStyle");
                    }
                }

                if (values[1] is ContentPresenter button)
                {
                    if (button.Name == "PlayIcon")
                    {
                        return Application.Current.FindResource(isPlaying ? "WhitePauseIcon" : "WhitePlayIcon");
                    }
                }

                if (values[1] is ContentControl cc)
                {
                    return Application.Current.FindResource(isPlaying ? "PauseTempl" : "PlayTempl");
                }
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PlayModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is PlayerMode playerMode)) return value;
            switch (playerMode)
            {
                case PlayerMode.Normal:
                    return Application.Current.FindResource($"ModeNormal{parameter}Templ");
                case PlayerMode.Random:
                    return Application.Current.FindResource($"ModeRandom{parameter}Templ");
                case PlayerMode.Loop:
                    return Application.Current.FindResource($"ModeLoop{parameter}Templ");
                case PlayerMode.LoopRandom:
                    return Application.Current.FindResource($"ModeLoopRandom{parameter}Templ");
                case PlayerMode.Single:
                    return Application.Current.FindResource($"ModeSingle{parameter}Templ");
                case PlayerMode.SingleLoop:
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
}
