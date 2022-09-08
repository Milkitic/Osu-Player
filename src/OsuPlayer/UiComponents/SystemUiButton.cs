using System.Windows;
using Milki.OsuPlayer.UiComponents.ButtonComponent;

namespace Milki.OsuPlayer.UiComponents;

public class SystemUiButton : UiButton
{
    public SystemUiButton()
    {
        this.Loaded += SystemButton_Loaded;
    }

    protected Window HostWindow { get; private set; }

    private void SystemButton_Loaded(object sender, RoutedEventArgs e)
    {
        if (HostWindow != null)
        {
            return;
        }

        HostWindow = Window.GetWindow(this);
    }
}

public class CloseUiButton : SystemUiButton
{
    public CloseUiButton()
    {
        this.Click += OnClick;
    }

    private void OnClick(object sender, RoutedEventArgs args)
    {
        HostWindow?.Close();
    }
}

public class MinUiButton : SystemUiButton
{
    public MinUiButton()
    {
        this.Click += OnClick;
    }

    private void OnClick(object sender, RoutedEventArgs args)
    {
        if (HostWindow != null)
        {
            HostWindow.WindowState = WindowState.Minimized;
        }
    }
}

public class MaxUiButton : SystemUiButton
{
    public static readonly DependencyProperty IsWindowMaxProperty = DependencyProperty.Register(nameof(IsWindowMax), typeof(bool), typeof(SystemButton), new PropertyMetadata(false));
    private bool _isSigned;

    public MaxUiButton()
    {
        this.Click += OnClick;
    }

    public bool IsWindowMax
    {
        get => (bool)GetValue(IsWindowMaxProperty);
        set => SetValue(IsWindowMaxProperty, value);
    }

    private void OnClick(object sender, RoutedEventArgs args)
    {
        if (HostWindow == null) return;
        
        if (!_isSigned)
        {
            HostWindow.StateChanged += delegate
            {
                if (HostWindow.WindowState == WindowState.Normal)
                {
                    IsWindowMax = false;
                }
                else if (HostWindow.WindowState == WindowState.Maximized)
                {
                    IsWindowMax = true;
                }
            };

            _isSigned = true;
        }

        if (HostWindow.WindowState == WindowState.Normal)
        {
            HostWindow.WindowState = WindowState.Maximized;
        }
        else if (HostWindow.WindowState == WindowState.Maximized)
        {
            HostWindow.WindowState = WindowState.Normal;
        }
    }
}