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

    private static readonly Geometry s_maximizeIcon = Geometry.Parse("M1,1 L11,1 11,11 1,11 Z M2,2 L2,10 10,10 10,2 Z");
    private static readonly Geometry s_restoreIcon = Geometry.Parse("M3,1 L11,1 11,9 9,9 9,3 3,3 Z M1,3 L9,3 9,11 1,11 Z M2,4 L2,10 8,10 8,4 Z");

    private Window? _hostWindow;
    private Path? _maxIcon;

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
        _maxIcon.Data = state == WindowState.Maximized ? s_restoreIcon : s_maximizeIcon;
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
