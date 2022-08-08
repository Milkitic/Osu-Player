using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Milki.OsuPlayer.UiComponents.ButtonComponent
{
    public class SystemButton : Button
    {
        protected Window HostWindow { get; private set; }

        public SystemButton()
        {
            this.Loaded += SystemButton_Loaded;
        }

        private void SystemButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (HostWindow != null)
            {
                return;
            }

            HostWindow = Window.GetWindow(this);
        }

        private static readonly SolidColorBrush DefaultMouseDownBrush =
            new SolidColorBrush(Color.FromArgb(48, 48, 48, 48));

        private static readonly SolidColorBrush DefaultMouseOverBrush =
            new SolidColorBrush(Color.FromArgb(32, 48, 48, 48));

        public ControlTemplate IconTemplate
        {
            get => (ControlTemplate)GetValue(IconTemplateProperty);
            set => SetValue(IconTemplateProperty, value);
        }

        public static readonly DependencyProperty IconTemplateProperty =
            DependencyProperty.Register(
                "IconTemplate",
                typeof(ControlTemplate),
                typeof(SystemButton),
                null);

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(
                "IconSize",
                typeof(double),
                typeof(SystemButton),
                new PropertyMetadata(24d)
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
                typeof(SystemButton),
                new PropertyMetadata(new Thickness())
            );

        public Brush MouseOverBackground
        {
            get => (Brush)GetValue(MouseOverBackgroundProperty);
            set => SetValue(MouseOverBackgroundProperty, value);
        }

        public static readonly DependencyProperty MouseOverBackgroundProperty =
            DependencyProperty.Register(
                "MouseOverBackground",
                typeof(Brush),
                typeof(SystemButton),
                new PropertyMetadata(DefaultMouseOverBrush)
            );

        public Brush MouseDownBackground
        {
            get => (Brush)GetValue(MouseDownBackgroundProperty);
            set => SetValue(MouseDownBackgroundProperty, value);
        }

        public static readonly DependencyProperty MouseDownBackgroundProperty =
            DependencyProperty.Register(
                "MouseDownBackground",
                typeof(Brush),
                typeof(SystemButton),
                new PropertyMetadata(DefaultMouseDownBrush)
            );

        static SystemButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SystemButton),
                new FrameworkPropertyMetadata(typeof(SystemButton)));
        }
    }
}