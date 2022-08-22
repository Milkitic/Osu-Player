using System.Globalization;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

public class MsToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        value ??= 0;
        var timeSpan = TimeSpan.FromMilliseconds(System.Convert.ToDouble(value));
        return timeSpan < TimeSpan.Zero
            ? $"-{timeSpan:mm\\:ss}"
            : $"{timeSpan:mm\\:ss}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}