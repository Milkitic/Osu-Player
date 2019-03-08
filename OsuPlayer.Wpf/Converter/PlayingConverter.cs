using Milky.OsuPlayer.Windows;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Converter
{
    public class PlayingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isPlaying = (bool)values[0];
            var window = (Window)values[1];
            if (window is LyricWindow lyricWindow)
            {
                return lyricWindow.FindResource(isPlaying ? "PauseButton" : "PlayButton");
            }

            if (window is MainWindow mainWindow)
            {
                return Application.Current.FindResource(isPlaying ? "PauseButtonStyle" : "PlayButtonStyle");
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
