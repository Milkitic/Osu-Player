using System;
using System.Globalization;
using System.Windows.Data;

namespace Milky.OsuPlayer.Converter
{
    class MarkdownConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (string)values[0];
            var nowV = (string)values[1];
            var newV = (string)values[2];
            var url = (string)values[3];
            return $"#### Current version\r\n" +
                   $"{nowV}\r\n" +
                   $"#### New version\r\n" +
                   $"{newV}\r\n" +
                   $"#### Release Page\r\n" +
                   $"{url}\r\n" +
                   $"#### Release Note\r\n" +
                   $"{s}\r\n\r\n" +
                   $"#### Update\r\n" +
                   $"**[Click here to update](update)**\r\n\r\n" +
                   $"---\r\n\r\n" +
                   $"*[Skip this version](ignore)*    *[Remind me later](later)*";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}