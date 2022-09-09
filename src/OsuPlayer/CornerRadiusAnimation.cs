#nullable enable

using System.Windows;
using System.Windows.Media.Animation;

namespace Milki.OsuPlayer;

internal class CornerRadiusAnimation : AnimationTimeline
{
    public static readonly DependencyProperty FromProperty = DependencyProperty.Register(
        nameof(From), typeof(CornerRadius), typeof(CornerRadiusAnimation), new PropertyMetadata(default(CornerRadius), PropertyChanged));

    public static readonly DependencyProperty ToProperty = DependencyProperty.Register(
        nameof(To), typeof(CornerRadius), typeof(CornerRadiusAnimation), new PropertyMetadata(default(CornerRadius), PropertyChanged));

    public static readonly DependencyProperty EasingFunctionProperty = DependencyProperty.Register(
        nameof(EasingFunction), typeof(IEasingFunction), typeof(CornerRadiusAnimation), new PropertyMetadata(default(IEasingFunction), PropertyChanged));

    private readonly ThicknessAnimation _thicknessAnimation = new();

    public IEasingFunction EasingFunction
    {
        get => (IEasingFunction)GetValue(EasingFunctionProperty);
        set => SetValue(EasingFunctionProperty, value);
    }

    public CornerRadius? From
    {
        get => (CornerRadius?)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }
    //public IEasingFunction EasingFunction
    //{
    //    get => _thicknessAnimation.EasingFunction;
    //    set => _thicknessAnimation.EasingFunction = value;
    //}

    //public CornerRadius? From
    //{
    //    get => _thicknessAnimation.From == null ? null : ThicknessToCornerRadius(_thicknessAnimation.From.Value);
    //    set => _thicknessAnimation.From = value == null ? null : CornerRadiusToThickness(value.Value);
    //}

    //public CornerRadius? To
    //{
    //    get => _thicknessAnimation.To == null ? null : ThicknessToCornerRadius(_thicknessAnimation.To.Value);
    //    set
    //    {
    //        if (value == null)
    //            _thicknessAnimation.To = null;
    //        else
    //            _thicknessAnimation.To = CornerRadiusToThickness(value.Value);
    //    }
    //}

    public override Type TargetPropertyType { get; } = typeof(CornerRadius);

    public CornerRadius? To
    {
        get => (CornerRadius?)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
    {
        var ov = (CornerRadius)defaultOriginValue;
        var dv = (CornerRadius)defaultDestinationValue;
        var thickness = (Thickness)_thicknessAnimation.GetCurrentValue(CornerRadiusToThickness(ov), CornerRadiusToThickness(dv),
            animationClock);
        return ThicknessToCornerRadius(thickness)!;
    }

    protected override Freezable CreateInstanceCore()
    {
        return new CornerRadiusAnimation();
    }

    private static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CornerRadiusAnimation cornerRadiusAnimation)
        {
            cornerRadiusAnimation._thicknessAnimation.EasingFunction = cornerRadiusAnimation.EasingFunction;
            cornerRadiusAnimation._thicknessAnimation.To = CornerRadiusToThickness(cornerRadiusAnimation.To);
            cornerRadiusAnimation._thicknessAnimation.From = CornerRadiusToThickness(cornerRadiusAnimation.From);
        }
    }

    private static CornerRadius? ThicknessToCornerRadius(Thickness? t)
    {
        if (t is null) return null;
        var thickness = t.Value;
        return new CornerRadius(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);
    }

    private static Thickness? CornerRadiusToThickness(CornerRadius? ov)
    {
        if (ov is null) return null;
        var cornerRadius = ov.Value;
        return new Thickness(cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight, cornerRadius.BottomLeft);
    }
}