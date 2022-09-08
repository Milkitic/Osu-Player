using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
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

public class CloseUiButton : SystemUiButton
{
    private const string ResourceCloseTemplString =
        "    <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Key=\"CloseTempl\">\r\n        <Canvas Width=\"1024\" Height=\"1024\">\r\n            <Path Fill=\"{TemplateBinding Foreground}\">\r\n                <Path.Data>\r\n                    <PathGeometry Figures=\"M886.592 841.344L557.248 512l329.36-329.36a32 32 0 1 0-45.264-45.232L512 466.752 182.656 137.408a32 32 0 1 0-45.264 45.232L466.752 512 137.408 841.344a32 32 0 1 0 45.232 45.264L512 557.248l329.36 329.36a32 32 0 1 0 45.232-45.264z\" FillRule=\"NonZero\" />\r\n                </Path.Data>\r\n            </Path>\r\n        </Canvas>\r\n    </ControlTemplate>";

    private static readonly ControlTemplate CloseTempl;
    private static readonly Brush StaticMouseOverBackground;
    private static readonly Brush StaticMouseDownBackground;

    static CloseUiButton()
    {
        CloseTempl = System.Windows.Markup.XamlReader.Parse(ResourceCloseTemplString) as ControlTemplate;
        StaticMouseOverBackground = (Brush)new BrushConverter().ConvertFrom("#F0F72F2F");
        StaticMouseDownBackground = (Brush)new BrushConverter().ConvertFrom("#A0C72828");
    }

    public CloseUiButton()
    {
        IconSize = 16;
        IconTemplate = CloseTempl;
        MouseOverBackground = StaticMouseOverBackground;
        MouseDownBackground = StaticMouseDownBackground;
        Click += OnClick;
    }

    private void OnClick(object sender, RoutedEventArgs args)
    {
        HostWindow?.Close();
    }
}

public class MinUiButton : SystemUiButton
{
    private const string ResourceMinimizeTemplString =
        "    <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Key=\"MinimizeTempl\">\r\n        <Canvas Width=\"1024\" Height=\"1024\">\r\n            <Path Fill=\"{TemplateBinding Foreground}\">\r\n                <Path.Data>\r\n                    <PathGeometry Figures=\"M797.291117 486.21473 224.18848 486.21473c-14.078647 0-25.469068 11.342326-25.469068 25.472138 0 14.028505 11.390421 25.471115 25.469068 25.471115l573.101613 0c14.07967 0 25.470091-11.441587 25.470091-25.471115C822.760185 497.557056 811.370787 486.21473 797.291117 486.21473z\" FillRule=\"NonZero\" />\r\n                </Path.Data>\r\n            </Path>\r\n        </Canvas>\r\n    </ControlTemplate>";

    private static readonly ControlTemplate MinimizeTempl;

    static MinUiButton()
    {
        MinimizeTempl = System.Windows.Markup.XamlReader.Parse(ResourceMinimizeTemplString) as ControlTemplate;
    }

    public MinUiButton()
    {
        IconMargin = new Thickness(0, 8, 0, 0);
        IconTemplate = MinimizeTempl;
        Click += OnClick;
    }

    private void OnClick(object sender, RoutedEventArgs args)
    {
        if (HostWindow != null)
        {
            HostWindow.WindowState = WindowState.Minimized;
        }
    }
}

public class MaxUiButton : SystemUiButton, INotifyPropertyChanged
{
    private const string ResourceRecoverTemplString = "    <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Key=\"RecoverTempl\">\r\n        <Canvas Width=\"1024\" Height=\"1024\">\r\n            <Path Fill=\"{TemplateBinding Foreground}\">\r\n                <Path.Data>\r\n                    <PathGeometry Figures=\"M512 1255.489906\" FillRule=\"NonZero\" />\r\n                </Path.Data>\r\n            </Path>\r\n            <Path Fill=\"{TemplateBinding Foreground}\">\r\n                <Path.Data>\r\n                    <PathGeometry Figures=\"M865.682191 310.085948l-554.675195 0c-14.634419 0-26.403358 11.973616-26.403358 26.710374L284.603638 423.681791l-92.309414 0c-14.634419 0-26.403358 11.973616-26.403358 26.710374l0 349.998001c0 14.634419 11.768939 26.505697 26.403358 26.505697l554.675195 0c14.634419 0 26.710374-11.871277 26.710374-26.505697L773.679792 713.30002l92.002399 0c14.634419 0 26.710374-11.871277 26.710374-26.505697l0-349.998001C892.392564 322.059564 880.31661 310.085948 865.682191 310.085948zM728.65081 781.86688 210.817509 781.86688 210.817509 468.710774l517.8333 0L728.65081 781.86688zM847.363582 668.271037l-73.68379 0L773.679792 450.392165c0-14.634419-12.075954-26.710374-26.710374-26.710374L329.530282 423.681791l0-68.56686 517.8333 0L847.363582 668.271037z\" FillRule=\"NonZero\" />\r\n                </Path.Data>\r\n            </Path>\r\n        </Canvas>\r\n    </ControlTemplate>";
    private const string ResourceMaximizeTemplString = "    <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Key=\"MaximizeTempl\">\r\n        <Canvas Width=\"1024\" Height=\"1024\">\r\n            <Path Fill=\"{TemplateBinding Foreground}\">\r\n                <Path.Data>\r\n                    <PathGeometry Figures=\"M204.8 256a51.2 51.2 0 0 0-51.2 51.2v409.6a51.2 51.2 0 0 0 51.2 51.2h614.4a51.2 51.2 0 0 0 51.2-51.2V307.2a51.2 51.2 0 0 0-51.2-51.2H204.8z m0-51.2h614.4a102.4 102.4 0 0 1 102.4 102.4v409.6a102.4 102.4 0 0 1-102.4 102.4H204.8a102.4 102.4 0 0 1-102.4-102.4V307.2a102.4 102.4 0 0 1 102.4-102.4z\" FillRule=\"NonZero\" />\r\n                </Path.Data>\r\n            </Path>\r\n        </Canvas>\r\n    </ControlTemplate>";

    public static readonly ControlTemplate MaximizeTempl;
    public static readonly ControlTemplate RecoverTempl;

    private bool _isSigned;
    private bool _isWindowMax;

    static MaxUiButton()
    {
        RecoverTempl = System.Windows.Markup.XamlReader.Parse(ResourceRecoverTemplString) as ControlTemplate;
        MaximizeTempl = System.Windows.Markup.XamlReader.Parse(ResourceMaximizeTemplString) as ControlTemplate;
    }

    public MaxUiButton()
    {
        IconTemplate = MaximizeTempl;
        Click += OnClick;
        Loaded += MaxUiButton_Loaded;
    }

    public bool IsWindowMax
    {
        get => _isWindowMax;
        set => SetField(ref _isWindowMax, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void MaxUiButton_Loaded(object sender, RoutedEventArgs e)
    {
        SignUpEvent();
        RecoverState();
    }

    private void OnClick(object sender, RoutedEventArgs args)
    {
        if (HostWindow == null) return;

        SignUpEvent();

        if (HostWindow.WindowState == WindowState.Normal)
        {
            HostWindow.WindowState = WindowState.Maximized;
        }
        else if (HostWindow.WindowState == WindowState.Maximized)
        {
            HostWindow.WindowState = WindowState.Normal;
        }
    }

    private void SignUpEvent()
    {
        if (_isSigned) return;
        if (HostWindow != null) HostWindow.StateChanged += (_, _) => RecoverState();

        _isSigned = true;
    }

    private void RecoverState()
    {
        if (HostWindow is null || HostWindow.WindowState == WindowState.Normal)
        {
            IsWindowMax = false;
            IconTemplate = MaximizeTempl;
            IconMargin = new Thickness(0);
            IconSize = 20;
        }
        else if (HostWindow.WindowState == WindowState.Maximized)
        {
            IsWindowMax = true;
            IconTemplate = RecoverTempl;
            IconMargin = new Thickness(0, 0, 0, 3);
            IconSize = 23;
        }
    }
}