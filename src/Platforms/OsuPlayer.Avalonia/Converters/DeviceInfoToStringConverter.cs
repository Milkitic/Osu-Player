using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using KeyAsio.Core.Audio;
using OsuPlayer.Utils;
using OsuPlayer.Lang;

namespace OsuPlayer.Converters;

public class DeviceInfoToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DeviceDescription b) return value;
        if (DeviceComparer.Instance.Equals(b, DeviceDescription.WasapiDefault))
            return I18NUtil.GetString(SRKeys.Ui_Sets_Content_SystemDefault);
        return $"({b.WavePlayerType}) {b.FriendlyName}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}
