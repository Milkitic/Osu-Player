using System.Globalization;
using System.Windows.Data;
using Milki.OsuPlayer.Utils;

namespace Milki.OsuPlayer.Converters;

public class LyricWindowShownConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var shown = (bool)value;
        if (System.Convert.ToString(parameter) == "string")
        {
            return shown ? I18NUtil.GetString("ui-closeDesktopLyric") : I18NUtil.GetString("ui-openDesktopLyric");
        }
        else if (System.Convert.ToString(parameter) == "bool")
        {
            return shown;
        }
        else return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}