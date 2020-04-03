using System;
using System.Globalization;
using System.Windows.Data;
using Milky.OsuPlayer.Utils;
using OsuPlayer.Devices;

namespace Milky.OsuPlayer.Converters
{
    public class DeviceInfoToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IDeviceInfo b)) return value;
            if (b.Equals(WasapiInfo.Default))
            {
                return I18NUtil.GetString("ui-sets-content-systemDefault");
            }

            return $"({b.OutputMethod}) {b.FriendlyName}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}