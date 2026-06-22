using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace OsuPlayer.Localization;

public sealed class I18NExtension : MarkupExtension
{
    public I18NExtension()
    {
    }

    public I18NExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string? Key { get; set; }

    [DynamicDependency(nameof(LocalizationService.Version), typeof(LocalizationService))]
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "The bound property (LocalizationService.Version) is preserved by [DynamicDependency].")]
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            return string.Empty;
        }

        return new Binding(nameof(LocalizationService.Version))
        {
            Mode = BindingMode.OneWay,
            Source = LocalizationService.Instance,
            Converter = I18NValueConverter.Instance,
            ConverterParameter = Key
        };
    }
}

public sealed class I18NValueConverter : IValueConverter
{
    public static readonly I18NValueConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return parameter is string key ? LocalizationService.Instance[key] : string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
