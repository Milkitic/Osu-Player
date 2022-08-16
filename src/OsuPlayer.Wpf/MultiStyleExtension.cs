using System.Windows;
using System.Windows.Markup;

namespace Milki.OsuPlayer.Wpf;

[MarkupExtensionReturnType(typeof(Style))]
public class MultiStyleExtension : MarkupExtension
{
    private readonly string[] _resourceKeys;

    public MultiStyleExtension(string inputResourceKeys)
    {
        if (inputResourceKeys == null)
        {
            throw new ArgumentNullException(nameof(inputResourceKeys));
        }

        _resourceKeys = inputResourceKeys.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (_resourceKeys.Length == 0)
        {
            throw new ArgumentException("No input resource keys specified.");
        }
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var resultStyle = new Style();

        foreach (var currentResourceKey in _resourceKeys)
        {
            if (new StaticResourceExtension(currentResourceKey).ProvideValue(serviceProvider) is Style
                currentStyle)
            {
                Merge(resultStyle, currentStyle);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Could not find style with resource key '{currentResourceKey}'.");
            }
        }

        return resultStyle;
    }

    public static void Merge(Style style1, Style style2)
    {
        if (style1 == null)
        {
            throw new ArgumentNullException(nameof(style1));
        }
        if (style2 == null)
        {
            throw new ArgumentNullException(nameof(style2));
        }

        if (style1.TargetType.IsAssignableFrom(style2.TargetType))
        {
            style1.TargetType = style2.TargetType;
        }

        if (style2.BasedOn != null)
        {
            Merge(style1, style2.BasedOn);
        }

        foreach (var currentSetter in style2.Setters)
        {
            style1.Setters.Add(currentSetter);
        }

        foreach (var currentTrigger in style2.Triggers)
        {
            style1.Triggers.Add(currentTrigger);
        }

        // This code is only needed when using DynamicResources.
        foreach (var key in style2.Resources.Keys)
        {
            style1.Resources[key] = style2.Resources[key];
        }
    }
}