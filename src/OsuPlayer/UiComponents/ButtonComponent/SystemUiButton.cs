using System.Windows;
using System.Windows.Media;

namespace Milki.OsuPlayer.UiComponents.ButtonComponent;

public class SystemUiButton : UiButton
{
    private static readonly Brush StaticMouseDownBackground;
    private static readonly Brush StaticMouseOverBackground;

    static SystemUiButton()
    {
        StaticMouseDownBackground = (Brush)new BrushConverter().ConvertFrom("#72727272");
        StaticMouseOverBackground = (Brush)new BrushConverter().ConvertFrom("#50727272");

        //BackgroundProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(Brushes.Transparent));
        //BorderBrushProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(Brushes.Transparent));
        //BorderThicknessProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(new Thickness(0)));
        //FontSizeProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(12d));
        //ForegroundProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(Brushes.White));
        //MouseOverBackgroundProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(StaticMouseOverBackground));
        //MouseOverForegroundProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(Brushes.White));
        //MouseDownBackgroundProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(StaticMouseDownBackground));
        //MouseDownForegroundProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(Brushes.White));
        //WidthProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(35d));
        //HeightProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(30d));
        //VerticalAlignmentProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(VerticalAlignment.Stretch));
        //VerticalContentAlignmentProperty.OverrideMetadata(typeof(SystemUiButton), new FrameworkPropertyMetadata(VerticalAlignment.Stretch));
    }

    public SystemUiButton()
    {
        Background = Brushes.Transparent;
        BorderBrush = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        FontSize = 12;
        Foreground = Brushes.White;
        MouseOverBackground = StaticMouseOverBackground;
        MouseOverForeground = Brushes.White;
        MouseDownBackground = StaticMouseDownBackground;
        MouseDownForeground = Brushes.White;
        Width = 35;
        Height = 30;
        VerticalAlignment = VerticalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        Loaded += Button_OnLoaded;
    }

    protected Window HostWindow { get; private set; }

    private void Button_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (HostWindow != null)
        {
            return;
        }

        HostWindow = Window.GetWindow(this);
    }
}