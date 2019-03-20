using Milky.OsuPlayer.ControlLibrary.Custom;
using Milky.OsuPlayer.Windows;
using Milky.WpfApi;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
