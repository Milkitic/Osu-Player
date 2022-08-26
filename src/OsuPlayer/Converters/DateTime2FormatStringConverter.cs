using System.Globalization;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

public class DateTime2FormatStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
        {
            try
            {
                if (dt == DateTime.MinValue) return "unknown";
                string format = parameter is string s ? s : null;
                return string.IsNullOrEmpty(format)
                    ? dt.ToString("g")
                    : dt.ToString(format);
            }
            catch (Exception e)
            {
                return dt.ToString(CultureInfo.CurrentCulture);
            }
        }

        return "unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}