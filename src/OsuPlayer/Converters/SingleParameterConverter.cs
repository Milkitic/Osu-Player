using System.Globalization;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

public class SingleParameterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolean;
        switch (parameter)
        {
            case string s:
                boolean = bool.Parse(s);
                break;
            case bool b:
                boolean = b;
                break;
            default:
                boolean = System.Convert.ToBoolean(parameter);
                break;
        }

        if (targetType == typeof(bool))
        {
            switch (value)
            {
                case string s:
                    return boolean ? !string.IsNullOrEmpty(s) : string.IsNullOrEmpty(s);
                default:
                    throw new NotImplementedException();
            }

        }

        throw new NotImplementedException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}