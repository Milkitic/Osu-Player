using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Milki.OsuPlayer.UiComponents.FrontDialogComponent;

/// <summary>
/// FrontDialogOverlay.xaml 的交互逻辑
/// </summary>
public partial class ContentDialog : UserControl
{
    private Point _mouseDownPos;
    public const int DefaultDialogWidth = 800;
    public const int DefaultDialogHeight = 500;

    #region Dependency property

    public bool AnimationEnabled
    {
        get => (bool)GetValue(AnimationEnabledProperty);
        set => SetValue(AnimationEnabledProperty, value);
    }

    public static readonly DependencyProperty AnimationEnabledProperty =
        DependencyProperty.Register(
            "AnimationEnabled",
            typeof(bool),
            typeof(ContentDialog),
            new PropertyMetadata(true));

    public bool IsDialogOpened
    {
        get => (bool)GetValue(IsDialogOpenedProperty);
        set => SetValue(IsDialogOpenedProperty, value);
    }

    public static readonly DependencyProperty IsDialogOpenedProperty =
        DependencyProperty.Register(
            "IsDialogOpened",
            typeof(bool),
            typeof(ContentDialog),
            new PropertyMetadata(false));

    public Brush OverlayBackground
    {
        get => (Brush)GetValue(OverlayBackgroundProperty);
        set => SetValue(OverlayBackgroundProperty, value);
    }

    public static readonly DependencyProperty OverlayBackgroundProperty =
        DependencyProperty.Register(
            "OverlayBackground",
            typeof(Brush),
            typeof(ContentDialog),
            new PropertyMetadata(new BrushConverter().ConvertFrom("#50333333")));

    public Brush DialogBackground
    {
        get => (Brush)GetValue(DialogBackgroundProperty);
        set => SetValue(DialogBackgroundProperty, value);
    }

    public static readonly DependencyProperty DialogBackgroundProperty =
        DependencyProperty.Register(
            "DialogBackground",
            typeof(Brush),
            typeof(ContentDialog),
            new PropertyMetadata(new BrushConverter().ConvertFrom("White")));

    public Brush DialogHeaderBackground
    {
        get => (Brush)GetValue(DialogHeaderBackgroundProperty);
        set => SetValue(DialogHeaderBackgroundProperty, value);
    }

    public static readonly DependencyProperty DialogHeaderBackgroundProperty =
        DependencyProperty.Register(
            "DialogHeaderBackground",
            typeof(Brush),
            typeof(ContentDialog),
            new PropertyMetadata(new BrushConverter().ConvertFrom("#10808080")));

    public Brush DialogHeaderForeground
    {
        get => (Brush)GetValue(DialogHeaderForegroundProperty);
        set => SetValue(DialogHeaderForegroundProperty, value);
    }

    public static readonly DependencyProperty DialogHeaderForegroundProperty =
        DependencyProperty.Register(
            "DialogHeaderForeground",
            typeof(Brush),
            typeof(ContentDialog),
            new PropertyMetadata(new BrushConverter().ConvertFrom("Black")));

    public double UiFontSize
    {
        get => (double)GetValue(UiFontSizeProperty);
        set => SetValue(UiFontSizeProperty, value);
    }

    public static readonly DependencyProperty UiFontSizeProperty =
        DependencyProperty.Register(
            "UiFontSize",
            typeof(double),
            typeof(ContentDialog),
            new PropertyMetadata(14d));

    public Brush DialogBorderBrush
    {
        get => (Brush)GetValue(DialogBorderBrushProperty);
        set => SetValue(DialogBorderBrushProperty, value);
    }

    public static readonly DependencyProperty DialogBorderBrushProperty =
        DependencyProperty.Register(
            "DialogBorderBrush",
            typeof(Brush),
            typeof(ContentDialog),
            new PropertyMetadata(new BrushConverter().ConvertFrom("#999999")));

    public Brush FooterBackground
    {
        get => (Brush)GetValue(FooterBackgroundProperty);
        set => SetValue(FooterBackgroundProperty, value);
    }

    public static readonly DependencyProperty FooterBackgroundProperty =
        DependencyProperty.Register(
            "FooterBackground",
            typeof(Brush),
            typeof(ContentDialog),
            new PropertyMetadata(new BrushConverter().ConvertFrom("Transparent")));
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            "Title",
            typeof(string),
            typeof(ContentDialog),
            new PropertyMetadata("标题"));

    public double DialogWidth
    {
        get => (double)GetValue(DialogWidthProperty);
        set => SetValue(DialogWidthProperty, value);
    }

    public static readonly DependencyProperty DialogWidthProperty =
        DependencyProperty.Register(
            "DialogWidth",
            typeof(double),
            typeof(ContentDialog),
            new FrameworkPropertyMetadata(800d));

    public double DialogHeight
    {
        get => (double)GetValue(DialogHeightProperty);
        set => SetValue(DialogHeightProperty, value);
    }

    public static readonly DependencyProperty DialogHeightProperty =
        DependencyProperty.Register(
            "DialogHeight",
            typeof(double),
            typeof(ContentDialog),
            new FrameworkPropertyMetadata(520d));

    public Thickness DialogPadding
    {
        get => (Thickness)GetValue(DialogPaddingProperty);
        set => SetValue(DialogPaddingProperty, value);
    }

    public static readonly DependencyProperty DialogPaddingProperty =
        DependencyProperty.Register(
            "DialogPadding",
            typeof(Thickness),
            typeof(ContentDialog),
            new FrameworkPropertyMetadata(new Thickness(10)));

    public Thickness DialogBorderThickness
    {
        get => (Thickness)GetValue(DialogBorderThicknessProperty);
        set => SetValue(DialogBorderThicknessProperty, value);
    }

    public static readonly DependencyProperty DialogBorderThicknessProperty =
        DependencyProperty.Register(
            "DialogBorderThickness",
            typeof(Thickness),
            typeof(ContentDialog),
            new FrameworkPropertyMetadata(new Thickness(1)));

    internal object? DialogContent
    {
        get => GetValue(DialogContentProperty);
        set => SetValue(DialogContentProperty, value);
    }

    public static readonly DependencyProperty DialogContentProperty =
        DependencyProperty.Register(
            "DialogContent",
            typeof(object),
            typeof(ContentDialog),
            new PropertyMetadata(null));

    public Visibility DialogVisibility
    {
        get => (Visibility)GetValue(DialogVisibilityProperty);
        set => SetValue(DialogVisibilityProperty, value);
    }

    public static readonly DependencyProperty DialogVisibilityProperty =
        DependencyProperty.Register(
            "DialogVisibility",
            typeof(Visibility),
            typeof(ContentDialog),
            new FrameworkPropertyMetadata(Visibility.Visible));

    public Style? CommonButtonStyle
    {
        get => (Style)GetValue(CommonButtonStyleProperty);
        set => SetValue(CommonButtonStyleProperty, value);
    }

    public static readonly DependencyProperty CommonButtonStyleProperty =
        DependencyProperty.Register(
            "CommonButtonStyle",
            typeof(Style),
            typeof(ContentDialog),
            new PropertyMetadata(null));

    #endregion
    public EventHandler<DialogClosingEventArgs>? OkAction { get; private set; }
    public EventHandler<DialogClosingEventArgs>? CancelAction { get; private set; }

    public static ContentDialog? Default { get; private set; }

    public ContentDialog()
    {
        InitializeComponent();
        if (CommonButtonStyle == null)
            CommonButtonStyle = (Style)FindResource("SettingsButton");
        if (Default == null) Default = this;
    }

    public void RaiseOk()
    {
        OK_Click(null, null);
    }

    public void RaiseCancel()
    {
        Cancel_Click(null, null);
    }

    public void ShowContent(object content, DialogOptions options,
        EventHandler<DialogClosingEventArgs>? okAction = null,
        EventHandler<DialogClosingEventArgs>? cancelAction = null)
    {
        Title = options.Title;
        DialogWidth = options.Width;
        DialogHeight = options.Height;
        OkAction = okAction;
        CancelAction = cancelAction;

        var canvasW = DialogBack.ActualWidth;
        var canvasH = DialogBack.ActualHeight;

        DialogVisibility = options.DialogVisible ? Visibility.Visible : Visibility.Hidden;
        Header.Visibility = options.ShowTitleBar ? Visibility.Visible : Visibility.Collapsed;
        Fotter.Visibility = options.ShowDialogButtons ? Visibility.Visible : Visibility.Collapsed;
        Canvas.SetLeft(Dialog, canvasW / 2 - options.Width / 2);
        Canvas.SetTop(Dialog, canvasH / 2 - options.Height / 2);
        DialogContent = content;
        IsDialogOpened = true;
    }

    public void ShowContent(object content,
        string title = "",
        double width = DefaultDialogWidth,
        double height = DefaultDialogHeight,
        EventHandler<DialogClosingEventArgs>? okAction = null,
        EventHandler<DialogClosingEventArgs>? cancelAction = null)
    {
        ShowContent(content, new DialogOptions
        {
            Title = title,
            Width = width,
            Height = height
        }, okAction, cancelAction);
    }

    public ContentDialog GetOrCreateSubOverlay()
    {
        foreach (UIElement element in DialogContainer.Children)
        {
            if (element is ContentDialog overlay1)
            {
                return overlay1;
            }
        }

        var overlay = new ContentDialog();
        DialogContainer.Children.Add(overlay);
        return overlay;
    }

    public async Task<bool?> ShowDialog(Window window)
    {
        var tcs = new TaskCompletionSource<bool?>();

        bool? dialogResult;
        if (IsDialogOpened)
        {
            dialogResult = window.ShowDialog();
        }
        else
        {
            ShowContent("", new DialogOptions { DialogVisible = false });
            dialogResult = window.ShowDialog();
            RaiseOk();
        }

        tcs.SetResult(dialogResult);
        return await tcs.Task;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs? e)
    {
        var arg = new DialogClosingEventArgs();
        CancelAction?.Invoke(this, arg);
        if (arg.Cancel)
        {
            return;
        }

        IsDialogOpened = false;
    }

    private void OK_Click(object? sender, RoutedEventArgs? e)
    {
        var arg = new DialogClosingEventArgs();
        OkAction?.Invoke(DialogContent, arg);
        if (arg.Cancel)
        {
            return;
        }

        IsDialogOpened = false;
    }

    private void Reset()
    {
        DialogContent = null;
    }

    private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DockPanel panel)
        {
            return;
        }

        _mouseDownPos = e.GetPosition(null);
        panel.CaptureMouse();
        panel.Cursor = Cursors.SizeAll;
    }

    private void DockPanel_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || sender is not DockPanel panel)
        {
            return;
        }

        var preX = Canvas.GetLeft(Dialog);
        var preY = Canvas.GetTop(Dialog);
        double dx = e.GetPosition(null).X - _mouseDownPos.X + preX;
        double dy = e.GetPosition(null).Y - _mouseDownPos.Y + preY;
        SetDialogPosition(dx, dy);
        _mouseDownPos = e.GetPosition(null);
    }

    private void DockPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DockPanel panel)
        {
            return;
        }

        panel.ReleaseMouseCapture();
        panel.Cursor = Cursors.Arrow;
    }

    public class DialogOptions
    {
        public double Width { get; set; } = DefaultDialogWidth;
        public double Height { get; set; } = DefaultDialogHeight;
        public string? Title { get; set; }
        public bool ShowTitleBar { get; set; } = true;
        public bool ShowDialogButtons { get; set; } = true;
        public bool DialogVisible { get; set; } = true;
    }

    private void DialogContainer_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!IsDialogOpened) return;
        var oldSize = e.PreviousSize;
        var boxX = Dialog.ActualWidth / 2 + Canvas.GetLeft(Dialog);
        var boxY = Dialog.ActualHeight / 2 + Canvas.GetTop(Dialog);
        var ratioX = boxX / oldSize.Width;
        var ratioY = boxY / oldSize.Height;
        var newSize = e.NewSize;
        var x = newSize.Width * ratioX - Dialog.ActualWidth / 2;
        var y = newSize.Height * ratioY - Dialog.ActualHeight / 2;
        SetDialogPosition(x, y);
    }

    private void SetDialogPosition(double x, double y)
    {
        if (x < 0) x = 0;
        if (x > DialogContainer.ActualWidth - Dialog.ActualWidth) x = DialogContainer.ActualWidth - Dialog.ActualWidth;
        if (y < 0) y = 0;
        if (y > DialogContainer.ActualHeight - Dialog.ActualHeight) y = DialogContainer.ActualHeight - Dialog.ActualHeight;
        Canvas.SetLeft(Dialog, x);
        Canvas.SetTop(Dialog, y);
    }
}