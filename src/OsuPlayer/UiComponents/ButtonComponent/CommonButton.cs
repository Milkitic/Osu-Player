using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Milki.OsuPlayer.UiComponents.ButtonComponent;

public class CommonButton : Button
{
    public static readonly DependencyProperty IconTemplateProperty = DependencyProperty.Register(nameof(IconTemplate), typeof(ControlTemplate), typeof(CommonButton), null);
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(CommonButton), new PropertyMetadata(new CornerRadius(2)));
    public static readonly DependencyProperty IconMarginProperty = DependencyProperty.Register(nameof(IconMargin), typeof(Thickness), typeof(CommonButton), new PropertyMetadata(new Thickness(0, 0, 8, 0)));
    public static readonly DependencyProperty IconOrientationProperty = DependencyProperty.Register(nameof(IconOrientation), typeof(Orientation), typeof(CommonButton), new PropertyMetadata(Orientation.Horizontal));
    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(CommonButton), new PropertyMetadata(24d));
    public static readonly DependencyProperty ShadowColorProperty = DependencyProperty.Register(nameof(ShadowColor), typeof(Color), typeof(CommonButton), new PropertyMetadata(Color.FromArgb(255, 205, 30, 93), null));
    public static readonly DependencyProperty ShadowOpacityProperty = DependencyProperty.Register(nameof(ShadowOpacity), typeof(double), typeof(CommonButton), new PropertyMetadata(0d, null));
    public static readonly DependencyProperty MouseOverBackgroundProperty = DependencyProperty.Register(nameof(MouseOverBackground), typeof(Brush), typeof(CommonButton), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty MouseOverForegroundProperty = DependencyProperty.Register(nameof(MouseOverForeground), typeof(Brush), typeof(CommonButton), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty MouseDownBackgroundProperty = DependencyProperty.Register(nameof(MouseDownBackground), typeof(Brush), typeof(CommonButton), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty MouseDownForegroundProperty = DependencyProperty.Register(nameof(MouseDownForeground), typeof(Brush), typeof(CommonButton), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty CheckedBackgroundProperty = DependencyProperty.Register(nameof(CheckedBackground), typeof(Brush), typeof(CommonButton), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty CheckedForegroundProperty = DependencyProperty.Register(nameof(CheckedForeground), typeof(Brush), typeof(CommonButton), new PropertyMetadata(default(Brush)));

    static CommonButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CommonButton),
            new FrameworkPropertyMetadata(typeof(CommonButton)));
    }

    public Brush CheckedBackground
    {
        get => (Brush)GetValue(CheckedBackgroundProperty);
        set => SetValue(CheckedBackgroundProperty, value);
    }

    public Brush CheckedForeground
    {
        get => (Brush)GetValue(CheckedForegroundProperty);
        set => SetValue(CheckedForegroundProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public Thickness IconMargin
    {
        get => (Thickness)GetValue(IconMarginProperty);
        set => SetValue(IconMarginProperty, value);
    }

    public Orientation IconOrientation
    {
        get => (Orientation)GetValue(IconOrientationProperty);
        set => SetValue(IconOrientationProperty, value);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public ControlTemplate IconTemplate
    {
        get => (ControlTemplate)GetValue(IconTemplateProperty);
        set => SetValue(IconTemplateProperty, value);
    }

    public Brush MouseDownBackground
    {
        get => (Brush)GetValue(MouseDownBackgroundProperty);
        set => SetValue(MouseDownBackgroundProperty, value);
    }

    public Brush MouseDownForeground
    {
        get => (Brush)GetValue(MouseDownForegroundProperty);
        set => SetValue(MouseDownForegroundProperty, value);
    }

    public Brush MouseOverBackground
    {
        get => (Brush)GetValue(MouseOverBackgroundProperty);
        set => SetValue(MouseOverBackgroundProperty, value);
    }

    public Brush MouseOverForeground
    {
        get => (Brush)GetValue(MouseOverForegroundProperty);
        set => SetValue(MouseOverForegroundProperty, value);
    }

    [Description("Shadow Color"), Category("Appearance"), DefaultValue(typeof(Color), "255, 205, 30, 93")]
    public Color ShadowColor
    {
        get => (Color)GetValue(ShadowColorProperty);
        set => SetValue(ShadowColorProperty, value);
    }

    [Description("Shadow Opcacity"), Category("Appearance"), DefaultValue(0d)]
    public double ShadowOpacity
    {
        get => (double)GetValue(ShadowOpacityProperty);
        set => SetValue(ShadowOpacityProperty, value);
    }
}