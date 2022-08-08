using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Milki.OsuPlayer.UiComponents.NotificationComponent.Converters
{
    internal class NotificationTypeConverter : IValueConverter
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (!(parameter is string str))
                {
                    return Visibility.Visible;
                }

                var hidStyle = Visibility.Collapsed;

                var s = str.Split(';');
                if (s.Length > 1)
                {
                    hidStyle = (Visibility)Enum.Parse(typeof(Visibility), s[1]);
                }

                var values = s[0].Split(',')
                    .Select(k => (NotificationOption.NotificationLevel)Enum.Parse(typeof(NotificationOption.NotificationLevel), k))
                    .ToArray();

                return value is NotificationOption.NotificationLevel actualType && values.Contains(actualType)
                    ? Visibility.Visible
                    : hidStyle;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}