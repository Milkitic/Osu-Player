using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace OsuPlayer.Converters;

public class IconColorConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 2)
        {
            var b1 = values[0] as IBrush;
            var b2 = values[1] as IBrush;
            if (b2 != null) return b2;
            if (b1 != null) return b1;
        }
        return null;
    }
}
