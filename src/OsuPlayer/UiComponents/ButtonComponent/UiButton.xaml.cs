using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Milki.OsuPlayer.UiComponents.ButtonComponent;

/// <summary>
/// UiButton.xaml 的交互逻辑
/// </summary>
public partial class UiButton : Button
{
    private static readonly Brush BrushBackground = (Brush)new BrushConverter().ConvertFrom("#f0f0f0");
    private static readonly Brush BrushForeground = (Brush)new BrushConverter().ConvertFrom("#484848");
    private static readonly Brush BrushBorder = (Brush)new BrushConverter().ConvertFrom("#999999");
    private static readonly Brush BrushMouseOverBackground = (Brush)new BrushConverter().ConvertFrom("#f3f5f5");
    private static readonly Brush BrushMouseOverForeground = (Brush)new BrushConverter().ConvertFrom("#484848");
    private static readonly Brush BrushMouseDownBackground = (Brush)new BrushConverter().ConvertFrom("#e8e8e8");
    private static readonly Brush BrushMouseDownForeground = (Brush)new BrushConverter().ConvertFrom("#323232");

    public static readonly DependencyProperty IconTemplateProperty = DependencyProperty.Register(nameof(IconTemplate), typeof(ControlTemplate), typeof(UiButton), null);
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(UiButton), new PropertyMetadata(new CornerRadius(0)));
    public static readonly DependencyProperty IconMarginProperty = DependencyProperty.Register(nameof(IconMargin), typeof(Thickness), typeof(UiButton), new PropertyMetadata(new Thickness(0, 0, 0, 0)));
    public static readonly DependencyProperty IconOrientationProperty = DependencyProperty.Register(nameof(IconOrientation), typeof(Orientation), typeof(UiButton), new PropertyMetadata(Orientation.Horizontal));
    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(UiButton), new PropertyMetadata(24d));
    public static readonly DependencyProperty ShadowBlurRadiusProperty = DependencyProperty.Register(nameof(ShadowBlurRadius), typeof(double), typeof(UiButton), new PropertyMetadata(0d));
    public static readonly DependencyProperty ShadowColorProperty = DependencyProperty.Register(nameof(ShadowColor), typeof(Color), typeof(UiButton), new PropertyMetadata(Color.FromArgb(255, 205, 30, 93), null));
    public static readonly DependencyProperty ShadowOpacityProperty = DependencyProperty.Register(nameof(ShadowOpacity), typeof(double), typeof(UiButton), new PropertyMetadata(0d, null));
    public static readonly DependencyProperty MouseOverBackgroundProperty = DependencyProperty.Register(nameof(MouseOverBackground), typeof(Brush), typeof(UiButton), new PropertyMetadata(BrushMouseOverBackground));
    public static readonly DependencyProperty MouseOverForegroundProperty = DependencyProperty.Register(nameof(MouseOverForeground), typeof(Brush), typeof(UiButton), new PropertyMetadata(BrushMouseOverForeground));
    public static readonly DependencyProperty MouseDownBackgroundProperty = DependencyProperty.Register(nameof(MouseDownBackground), typeof(Brush), typeof(UiButton), new PropertyMetadata(BrushMouseDownBackground));
    public static readonly DependencyProperty MouseDownForegroundProperty = DependencyProperty.Register(nameof(MouseDownForeground), typeof(Brush), typeof(UiButton), new PropertyMetadata(BrushMouseDownForeground));
    public static readonly DependencyProperty CheckedBackgroundProperty = DependencyProperty.Register(nameof(CheckedBackground), typeof(Brush), typeof(UiButton), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty CheckedForegroundProperty = DependencyProperty.Register(nameof(CheckedForeground), typeof(Brush), typeof(UiButton), new PropertyMetadata(default(Brush)));

    public UiButton()
    {
        Padding = new Thickness(0);
        Margin = new Thickness(0);
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        HorizontalContentAlignment = HorizontalAlignment.Center;
        VerticalContentAlignment = VerticalAlignment.Center;
        Background = BrushBackground;
        Foreground = BrushForeground;
        BorderBrush = BrushBorder;
        InitializeComponent();
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

    public double ShadowBlurRadius
    {
        get => (double)GetValue(ShadowBlurRadiusProperty);
        set => SetValue(ShadowBlurRadiusProperty, value);
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