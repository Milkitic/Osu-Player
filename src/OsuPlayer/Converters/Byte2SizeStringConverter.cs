using System.Globalization;
using System.Windows.Data;
using Milki.OsuPlayer.Shared.Utils;

namespace Milki.OsuPlayer.Converters;

public class Byte2SizeStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long val = 0;
        try
        {
            val = System.Convert.ToInt64(value);
        }
        catch
        {
            // ignored
        }

        return SharedUtils.SizeSuffix(val, 2);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}