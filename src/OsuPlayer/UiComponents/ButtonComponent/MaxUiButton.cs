using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Milki.OsuPlayer.UiComponents.ButtonComponent;

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