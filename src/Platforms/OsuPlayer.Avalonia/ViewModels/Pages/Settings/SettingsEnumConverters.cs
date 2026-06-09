using System;
using System.Globalization;
using Avalonia.Data.Converters;
using OsuPlayer.Shared.Models;
using BindingOperations = Avalonia.Data.BindingOperations;

namespace OsuPlayer.ViewModels.Pages.Settings;

public class LyricSourceToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LyricSource src && parameter is LyricSource expected)
            return src == expected;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is LyricSource expected)
            return expected;
        return BindingOperations.DoNothing;
    }
}

public class LyricProvideTypeToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LyricProvideType t && parameter is LyricProvideType expected)
            return t == expected;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is LyricProvideType expected)
            return expected;
        return BindingOperations.DoNothing;
    }
}

public class ExportNamingToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ExportNamingStyle t && parameter is ExportNamingStyle expected)
            return t == expected;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is ExportNamingStyle expected)
            return expected;
        return BindingOperations.DoNothing;
    }
}

public class ExportGroupToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ExportGroupStyle t && parameter is ExportGroupStyle expected)
            return t == expected;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is ExportGroupStyle expected)
            return expected;
        return BindingOperations.DoNothing;
    }
}
