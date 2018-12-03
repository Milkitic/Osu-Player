using Milkitic.OsuPlayer.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Milkitic.OsuPlayer.Converter
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
