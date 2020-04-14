using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace Milky.OsuPlayer.UiComponents.NotificationComponent.Converters
{
    internal class NotificationTypeToCursorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is NotificationOption.NotificationLevel actualType && actualType == NotificationOption.NotificationLevel.Alert
                ? Cursors.Hand
                : Cursors.Arrow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}