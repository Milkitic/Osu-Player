using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Milky.OsuPlayer.Control
{
    public class CommonButton : Button
    {
        public ControlTemplate IconTemplate
        {
            get => (ControlTemplate)GetValue(IconTemplateProperty);
            set => SetValue(IconTemplateProperty, value);
        }

        public static readonly DependencyProperty IconTemplateProperty =
            DependencyProperty.Register(
                "IconTemplate",
                typeof(ControlTemplate),
                typeof(CommonButton),
                null
            );

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(CommonButton),
                new PropertyMetadata(new CornerRadius(2))
            );

        public Thickness IconMargin
        {
            get => (Thickness)GetValue(IconMarginProperty);
            set => SetValue(IconMarginProperty, value);
        }

        public static readonly DependencyProperty IconMarginProperty =
            DependencyProperty.Register(
                "IconMargin",
                typeof(Thickness),
                typeof(CommonButton),
                new PropertyMetadata(new Thickness(0, 0, 8, 0))
            );

        public Orientation IconOrientation
        {
            get => (Orientation)GetValue(IconOrientationProperty);
            set => SetValue(IconOrientationProperty, value);
        }

        public static readonly DependencyProperty IconOrientationProperty =
            DependencyProperty.Register(
                "IconOrientation",
                typeof(Orientation),
                typeof(CommonButton),
                new PropertyMetadata(Orientation.Horizontal)
            );

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(
                "IconSize",
                typeof(double),
                typeof(CommonButton),
                new PropertyMetadata(24d)
            );

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

        public static readonly DependencyProperty MouseOverBackgroundProperty = DependencyProperty.Register("MouseOverBackground", typeof(Brush), typeof(CommonButton), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty MouseOverForegroundProperty = DependencyProperty.Register("MouseOverForeground", typeof(Brush), typeof(CommonButton), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty MouseDownBackgroundProperty = DependencyProperty.Register("MouseDownBackground", typeof(Brush), typeof(CommonButton), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty MouseDownForegroundProperty = DependencyProperty.Register("MouseDownForeground", typeof(Brush), typeof(CommonButton), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty CheckedBackgroundProperty = DependencyProperty.Register("CheckedBackground", typeof(Brush), typeof(CommonButton), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty CheckedForegroundProperty = DependencyProperty.Register("CheckedForeground", typeof(Brush), typeof(CommonButton), new PropertyMetadata(default(Brush)));

        private static Dictionary<string, List<CommonButton>> Scopes { get; } =
            new Dictionary<string, List<CommonButton>>();

        static CommonButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CommonButton), new FrameworkPropertyMetadata(typeof(CommonButton)));
        }
    }
}