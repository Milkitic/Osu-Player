using System;
using System.Globalization;
using System.Windows.Data;

namespace Loaders.Converters
{
    /// <summary>
    /// Applies a linear function to the value passed as a parameter.
    /// f(x) = a*x + b
    /// </summary>
    internal class LinearConverter : IValueConverter
    {
        public double A { get; set; } = 1;
        public double B { get; set; } = 0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => A*System.Convert.ToInt32(value) + B;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (System.Convert.ToInt32(value) - B) / A;
    }
}
