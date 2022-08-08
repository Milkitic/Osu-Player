using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Milki.OsuPlayer.UiComponents.NotificationComponent.Converters
{
    internal class MixColorConverter : IValueConverter
    {
        private static readonly Dictionary<Color, Brush> Brushes = new Dictionary<Color, Brush>();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush b)
            {
                var c = b.Color;
                var newC = (c.R + c.G + c.B) / 3f > 128
                    ? Color.FromArgb(c.A, Darker(c.R), Darker(c.G), Darker(c.B))
                    : Color.FromArgb(c.A, Lighter(c.R), Lighter(c.G), Lighter(c.B));
                if (!Brushes.ContainsKey(newC))
                {
                    Brushes.Add(newC, new SolidColorBrush(newC));
                }

                return Brushes[newC];
            }

            return value;
        }

        private byte Darker(byte b, double ratio = 0.15)
        {
            var newVal = b - b * ratio;
            return newVal < 0 ? (byte)0 : (byte)newVal;
        }

        private byte Lighter(byte b, double ratio = 0.15)
        {
            var newVal = b + b * ratio;
            return newVal > 255 ? (byte)255 : (byte)newVal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}