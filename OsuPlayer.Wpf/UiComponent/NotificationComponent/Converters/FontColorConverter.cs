using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Milky.OsuPlayer.UiComponent.NotificationComponent.Converters
{
    internal class FontColorConverter : IValueConverter
    {
        private static readonly Dictionary<Color, Brush> Brushes = new Dictionary<Color, Brush>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush b)
            {
                var c = b.Color;
                var newC = (c.R + c.G + c.B) / 3f > 128
                    ? Color.FromArgb(255, 32, 32, 32)
                    : Color.FromArgb(255, 240, 240, 240);

                if (!Brushes.ContainsKey(newC))
                {
                    Brushes.Add(newC, new SolidColorBrush(newC));
                }

                return Brushes[newC];
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}