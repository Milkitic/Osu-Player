using System.Globalization;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

public class TabHeaderLineX1Converter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (double?)value * (1 - HeaderParams.Multiplier) / 2;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}