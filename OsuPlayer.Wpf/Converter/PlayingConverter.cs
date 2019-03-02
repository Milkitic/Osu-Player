using System;
using System.Globalization;
using System.Windows.Data;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.Converter
{
    public class PlayingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isPlaying = (bool)values[0];
            var window = (LyricWindow)values[1];
            return window.MainGrid.FindResource(isPlaying ? "PauseButton" : "PlayButton");
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
