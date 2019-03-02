using System;
using System.Globalization;
using System.Windows.Data;
using osu.Shared;

namespace Milky.OsuPlayer.Converter
{
    class VersionToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var sb = (GameMode?)value;
            if (sb == null) return "";
            switch (sb)
            {
                case GameMode.Standard:
                    return "圈";
                case GameMode.Taiko:
                    return "鼓";
                case GameMode.CatchTheBeat:
                    return "果";
                case GameMode.Mania:
                    return "键";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
