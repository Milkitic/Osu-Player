using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters
{
    public class MiniWindowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMini)
            {
                var main = Application.Current.MainWindow;
                if (main != null)
                {
                    var width = isMini ? 360 : 960;
                    var height = isMini ? 48 : 720;
                    var minWidth = isMini ? 360 : 840;
                    var minHeight = isMini ? 48 : 98;
                    main.MinWidth = minWidth;
                    main.MinHeight = minHeight;
                    main.Width = width;
                    main.Height = height;
                    Debug.WriteLine("Window size is forced changed");
                }

                return isMini;
            }

            throw new ArgumentOutOfRangeException(nameof(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
