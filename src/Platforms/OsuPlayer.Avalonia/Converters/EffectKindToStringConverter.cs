using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Converters;

/// <summary>
/// Converts a <see cref="DirectXEffectKind"/> value to its localised
/// <c>Description</c> attribute. Used in the settings page combo box
/// so the user sees a Chinese label and we keep the underlying value
/// strongly typed.
/// </summary>
public sealed class EffectKindToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DirectXEffectKind kind) return string.Empty;
        var name = kind.ToString();
        var field = typeof(DirectXEffectKind).GetField(name, BindingFlags.Public | BindingFlags.Static);
        if (field == null) return name;
        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? name;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // The combo box binds to DirectXEffectKind values directly via
        // the EffectKinds property; this converter is one-way.
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}
