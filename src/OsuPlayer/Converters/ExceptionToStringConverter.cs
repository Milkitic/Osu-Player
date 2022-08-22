﻿using System.Globalization;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

public class ExceptionToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Exception e)
        {
            return e.ToString();
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}