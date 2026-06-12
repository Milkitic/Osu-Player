using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

namespace OsuPlayer.Controls;

public class CommonButton : Button
{
    public static readonly StyledProperty<IControlTemplate?> IconTemplateProperty =
        AvaloniaProperty.Register<CommonButton, IControlTemplate?>(nameof(IconTemplate));

    public static readonly StyledProperty<Thickness> IconMarginProperty =
        AvaloniaProperty.Register<CommonButton, Thickness>(nameof(IconMargin), new Thickness(0, 0, 8, 0));

    public static readonly StyledProperty<Orientation> IconOrientationProperty =
        AvaloniaProperty.Register<CommonButton, Orientation>(nameof(IconOrientation), Orientation.Horizontal);

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<CommonButton, double>(nameof(IconSize), 24d);

    public static readonly StyledProperty<IBrush?> MouseOverBackgroundProperty =
        AvaloniaProperty.Register<CommonButton, IBrush?>(nameof(MouseOverBackground));

    public static readonly StyledProperty<IBrush?> MouseOverForegroundProperty =
        AvaloniaProperty.Register<CommonButton, IBrush?>(nameof(MouseOverForeground));

    public static readonly StyledProperty<IBrush?> MouseDownBackgroundProperty =
        AvaloniaProperty.Register<CommonButton, IBrush?>(nameof(MouseDownBackground));

    public static readonly StyledProperty<IBrush?> MouseDownForegroundProperty =
        AvaloniaProperty.Register<CommonButton, IBrush?>(nameof(MouseDownForeground));

    public static readonly StyledProperty<IBrush?> CheckedBackgroundProperty =
        AvaloniaProperty.Register<CommonButton, IBrush?>(nameof(CheckedBackground));

    public static readonly StyledProperty<IBrush?> CheckedForegroundProperty =
        AvaloniaProperty.Register<CommonButton, IBrush?>(nameof(CheckedForeground));

    public static readonly StyledProperty<Color> ShadowColorProperty =
        AvaloniaProperty.Register<CommonButton, Color>(nameof(ShadowColor), Color.FromRgb(0xCD, 0x1E, 0x5D));

    public static readonly StyledProperty<double> ShadowOpacityProperty =
        AvaloniaProperty.Register<CommonButton, double>(nameof(ShadowOpacity), 0d);

    public static readonly StyledProperty<BoxShadows> BoxShadowProperty =
        AvaloniaProperty.Register<CommonButton, BoxShadows>(nameof(BoxShadow), new BoxShadows());

    public BoxShadows BoxShadow
    {
        get => GetValue(BoxShadowProperty);
        set => SetValue(BoxShadowProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShadowColorProperty || change.Property == ShadowOpacityProperty)
        {
            UpdateBoxShadow();
        }
    }

    public CommonButton()
    {
        UpdateBoxShadow();
    }

    private void UpdateBoxShadow()
    {
        var opacity = ShadowOpacity;
        if (opacity <= 0)
        {
            BoxShadow = new BoxShadows();
        }
        else
        {
            var color = ShadowColor;
            var alphaColor = Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B);
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 10,
                Color = alphaColor,
                OffsetX = 0,
                OffsetY = 1
            });
        }
    }

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

    public Color ShadowColor
    {
        get => GetValue(ShadowColorProperty);
        set => SetValue(ShadowColorProperty, value);
    }

    public double ShadowOpacity
    {
        get => GetValue(ShadowOpacityProperty);
        set => SetValue(ShadowOpacityProperty, value);
    }
}
