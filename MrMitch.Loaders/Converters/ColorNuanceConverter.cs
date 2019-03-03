using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Loaders.Converters
{
    internal class ColorNuanceConverter : IValueConverter
    {
        public int Nuance { get; set; }

        #region Implementation of IValueConverter

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var old = (Color) value;
            var r = old.R + Nuance;
            var g = old.G + Nuance;
            var b = old.B + Nuance;
            
            r = r > 255 ? 255 : (r < 0 ? 0 : r);
            g = g > 255 ? 255 : (g < 0 ? 0 : g);
            b = b > 255 ? 255 : (b < 0 ? 0 : b);

            return Color.FromArgb(old.A, (byte)r, (byte)g, (byte)b);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
