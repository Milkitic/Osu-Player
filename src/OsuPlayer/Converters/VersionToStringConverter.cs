using System.Globalization;
using System.Windows.Data;
using Coosu.Beatmap.Sections.GamePlay;

namespace Milki.OsuPlayer.Converters;

class VersionToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GameMode sb)
        {
            return sb switch
            {
                GameMode.Circle => "圈",
                GameMode.Taiko => "鼓",
                GameMode.Catch => "果",
                GameMode.Mania => "键",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}