﻿using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

public class NullToHiddenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is null ? Visibility.Hidden : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}