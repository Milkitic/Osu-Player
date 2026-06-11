using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace OsuPlayer.Views.UserControls;

public partial class WindowTitleBar : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<WindowTitleBar, string?>(nameof(Title));

    public static readonly StyledProperty<object?> LeftContentProperty =
        AvaloniaProperty.Register<WindowTitleBar, object?>(nameof(LeftContent));

    public static readonly StyledProperty<object?> RightContentProperty =
        AvaloniaProperty.Register<WindowTitleBar, object?>(nameof(RightContent));

    public static readonly StyledProperty<Thickness> LeftAreaMarginProperty =
        AvaloniaProperty.Register<WindowTitleBar, Thickness>(nameof(LeftAreaMargin), new Thickness(8, 0, 0, 0));

    public static readonly StyledProperty<Thickness> RightAreaMarginProperty =
        AvaloniaProperty.Register<WindowTitleBar, Thickness>(nameof(RightAreaMargin), new Thickness(0, 0, 1, 0));

    public static readonly StyledProperty<bool> ShowMinimizeProperty =
        AvaloniaProperty.Register<WindowTitleBar, bool>(nameof(ShowMinimize), true);

    public static readonly StyledProperty<bool> ShowMaximizeProperty =
        AvaloniaProperty.Register<WindowTitleBar, bool>(nameof(ShowMaximize), true);

    public static readonly StyledProperty<bool> ShowCloseProperty =
        AvaloniaProperty.Register<WindowTitleBar, bool>(nameof(ShowClose), true);

    private static readonly Geometry s_maximizeIcon = Geometry.Parse("M204.8 256a51.2 51.2 0 0 0-51.2 51.2v409.6a51.2 51.2 0 0 0 51.2 51.2h614.4a51.2 51.2 0 0 0 51.2-51.2V307.2a51.2 51.2 0 0 0-51.2-51.2H204.8z m0-51.2h614.4a102.4 102.4 0 0 1 102.4 102.4v409.6a102.4 102.4 0 0 1-102.4 102.4H204.8a102.4 102.4 0 0 1-102.4-102.4V307.2a102.4 102.4 0 0 1 102.4-102.4z");
    private static readonly Geometry s_restoreIcon = Geometry.Parse("M512 1255.489906 M865.682191 310.085948l-554.675195 0c-14.634419 0-26.403358 11.973616-26.403358 26.710374L284.603638 423.681791l-92.309414 0c-14.634419 0-26.403358 11.973616-26.403358 26.710374l0 349.998001c0 14.634419 11.768939 26.505697 26.403358 26.505697l554.675195 0c14.634419 0 26.710374-11.871277 26.710374-26.505697L773.679792 713.30002l92.002399 0c14.634419 0 26.710374-11.871277 26.710374-26.505697l0-349.998001C892.392564 322.059564 880.31661 310.085948 865.682191 310.085948zM728.65081 781.86688 210.817509 781.86688 210.817509 468.710774l517.8333 0L728.65081 781.86688zM847.363582 668.271037l-73.68379 0L773.679792 450.392165c0-14.634419-12.075954-26.710374-26.710374-26.710374L329.530282 423.681791l0-68.56686 517.8333 0L847.363582 668.271037z");

    private Window? _hostWindow;
    private Path? _maxIcon;
    private Viewbox? _maxIconViewbox;

    public WindowTitleBar()
    {
        InitializeComponent();
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? LeftContent
    {
        get => GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public Thickness LeftAreaMargin
    {
        get => GetValue(LeftAreaMarginProperty);
        set => SetValue(LeftAreaMarginProperty, value);
    }

    public Thickness RightAreaMargin
    {
        get => GetValue(RightAreaMarginProperty);
        set => SetValue(RightAreaMarginProperty, value);
    }

    public bool ShowMinimize
    {
        get => GetValue(ShowMinimizeProperty);
        set => SetValue(ShowMinimizeProperty, value);
    }

    public bool ShowMaximize
    {
        get => GetValue(ShowMaximizeProperty);
        set => SetValue(ShowMaximizeProperty, value);
    }

    public bool ShowClose
    {
        get => GetValue(ShowCloseProperty);
        set => SetValue(ShowCloseProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _maxIcon = this.FindControl<Path>("PART_MaxIcon");
        _maxIconViewbox = this.FindControl<Viewbox>("PART_MaxIconViewbox");
        _hostWindow = TopLevel.GetTopLevel(this) as Window;
        if (_hostWindow != null)
        {
            _hostWindow.PropertyChanged += HostWindow_PropertyChanged;
            UpdateMaxIcon(_hostWindow.WindowState);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_hostWindow != null)
        {
            _hostWindow.PropertyChanged -= HostWindow_PropertyChanged;
            _hostWindow = null;
        }
        base.OnDetachedFromVisualTree(e);
    }

    private void HostWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty && sender is Window w)
        {
            UpdateMaxIcon(w.WindowState);
        }
    }

    private void UpdateMaxIcon(WindowState state)
    {
        if (_maxIcon == null) return;
        var isMaximized = state == WindowState.Maximized;
        _maxIcon.Data = isMaximized ? s_restoreIcon : s_maximizeIcon;
        if (_maxIconViewbox == null) return;
        _maxIconViewbox.Width = isMaximized ? 23 : 20;
        _maxIconViewbox.Height = isMaximized ? 23 : 20;
        _maxIconViewbox.Margin = isMaximized ? new Thickness(0, 0, 0, 3) : new Thickness(0);
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        if (_hostWindow != null) _hostWindow.WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        if (_hostWindow == null) return;
        _hostWindow.WindowState = _hostWindow.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        _hostWindow?.Close();
    }
}
