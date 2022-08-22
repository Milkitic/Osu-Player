using System.Globalization;
using System.Windows.Data;
using Milki.OsuPlayer.Utils;

namespace Milki.OsuPlayer.Converters;

public class LyricWindowLockedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var locked = (bool)value;
        return locked ? I18NUtil.GetString("ui-unlockLyric") : I18NUtil.GetString("ui-lockLyric");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}