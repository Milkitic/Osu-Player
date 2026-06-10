using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

namespace OsuPlayer.Controls;

public class SwitchRadio : RadioButton
{
    public static readonly StyledProperty<IControlTemplate?> IconTemplateProperty =
        AvaloniaProperty.Register<SwitchRadio, IControlTemplate?>(nameof(IconTemplate));

    public static readonly StyledProperty<Thickness> IconMarginProperty =
        AvaloniaProperty.Register<SwitchRadio, Thickness>(nameof(IconMargin), new Thickness(0, 0, 8, 0));

    public static readonly StyledProperty<Orientation> IconOrientationProperty =
        AvaloniaProperty.Register<SwitchRadio, Orientation>(nameof(IconOrientation), Orientation.Horizontal);

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<SwitchRadio, double>(nameof(IconSize), 24d);

    public static readonly StyledProperty<IBrush?> IconColorProperty =
        AvaloniaProperty.Register<SwitchRadio, IBrush?>(nameof(IconColor));

    public static readonly StyledProperty<IBrush?> MouseOverBackgroundProperty =
        AvaloniaProperty.Register<SwitchRadio, IBrush?>(nameof(MouseOverBackground));

    public static readonly StyledProperty<IBrush?> MouseOverForegroundProperty =
        AvaloniaProperty.Register<SwitchRadio, IBrush?>(nameof(MouseOverForeground));

    public static readonly StyledProperty<IBrush?> MouseOverIconColorProperty =
        AvaloniaProperty.Register<SwitchRadio, IBrush?>(nameof(MouseOverIconColor));

    public static readonly StyledProperty<IBrush?> MouseDownBackgroundProperty =
        AvaloniaProperty.Register<SwitchRadio, IBrush?>(nameof(MouseDownBackground));

    public static readonly StyledProperty<IBrush?> MouseDownForegroundProperty =
        AvaloniaProperty.Register<SwitchRadio, IBrush?>(nameof(MouseDownForeground));

    public static readonly StyledProperty<IBrush?> MouseDownIconColorProperty =
        AvaloniaProperty.Register<SwitchRadio, IBrush?>(nameof(MouseDownIconColor));

    public static readonly StyledProperty<IBrush?> CheckedBackgroundProperty =
        AvaloniaProperty.Register<SwitchRadio, IBrush?>(nameof(CheckedBackground));

    public static readonly StyledProperty<IBrush?> CheckedForegroundProperty =
        AvaloniaProperty.Register<SwitchRadio, IBrush?>(nameof(CheckedForeground));

    public static readonly StyledProperty<IBrush?> CheckedIconColorProperty =
        AvaloniaProperty.Register<SwitchRadio, IBrush?>(nameof(CheckedIconColor));

    public IControlTemplate? IconTemplate
    {
        get => GetValue(IconTemplateProperty);
        set => SetValue(IconTemplateProperty, value);
    }

    public Thickness IconMargin
    {
        get => GetValue(IconMarginProperty);
        set => SetValue(IconMarginProperty, value);
    }

    public Orientation IconOrientation
    {
        get => GetValue(IconOrientationProperty);
        set => SetValue(IconOrientationProperty, value);
    }

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public IBrush? IconColor
    {
        get => GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public IBrush? MouseOverBackground
    {
        get => GetValue(MouseOverBackgroundProperty);
        set => SetValue(MouseOverBackgroundProperty, value);
    }

    public IBrush? MouseOverForeground
    {
        get => GetValue(MouseOverForegroundProperty);
        set => SetValue(MouseOverForegroundProperty, value);
    }

    public IBrush? MouseOverIconColor
    {
        get => GetValue(MouseOverIconColorProperty);
        set => SetValue(MouseOverIconColorProperty, value);
    }

    public IBrush? MouseDownBackground
    {
        get => GetValue(MouseDownBackgroundProperty);
        set => SetValue(MouseDownBackgroundProperty, value);
    }

    public IBrush? MouseDownForeground
    {
        get => GetValue(MouseDownForegroundProperty);
        set => SetValue(MouseDownForegroundProperty, value);
    }

    public IBrush? MouseDownIconColor
    {
        get => GetValue(MouseDownIconColorProperty);
        set => SetValue(MouseDownIconColorProperty, value);
    }

    public IBrush? CheckedBackground
    {
        get => GetValue(CheckedBackgroundProperty);
        set => SetValue(CheckedBackgroundProperty, value);
    }

    public IBrush? CheckedForeground
    {
        get => GetValue(CheckedForegroundProperty);
        set => SetValue(CheckedForegroundProperty, value);
    }

    public IBrush? CheckedIconColor
    {
        get => GetValue(CheckedIconColorProperty);
        set => SetValue(CheckedIconColorProperty, value);
    }
}
