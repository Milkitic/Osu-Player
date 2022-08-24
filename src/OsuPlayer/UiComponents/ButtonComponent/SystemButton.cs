using System.Windows;

namespace Milki.OsuPlayer.UiComponents.ButtonComponent;

public class SystemButton : CommonButton
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

    static SystemButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SystemButton),
            new FrameworkPropertyMetadata(typeof(SystemButton)));
    }
}