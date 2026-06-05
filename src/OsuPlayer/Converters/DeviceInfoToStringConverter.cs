using System;
using System.Globalization;
using System.Windows.Data;
using KeyAsio.Core.Audio;
using OsuPlayer.Utils;

namespace OsuPlayer.Converters
{
    public class DeviceInfoToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DeviceDescription b)) return value;
            if (DeviceComparer.Instance.Equals(b, DeviceDescription.WasapiDefault))
            {
                return I18NUtil.GetString("ui-sets-content-systemDefault");
            }

            return $"({b.WavePlayerType}) {b.FriendlyName}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
