#nullable enable

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Anotar.NLog;
using Milki.OsuPlayer.Wpf.Dependency;

namespace Milki.OsuPlayer.UiComponents.RadioButtonComponent;

public class SwitchRadio : RadioButton
{
    private Action<FrameworkElement>? _loadedAction;
    private Page? _pageInstance;
    protected FrameworkElement? HostContainer { get; private set; }

    public SwitchRadio()
    {
        Loaded += (sender, e) =>
        {
            if (HostContainer != null)
            {
                return;
            }

            HostContainer = this.FindParentObjects(typeof(Page), typeof(Window));
        };

        Checked += (sender, e) => NavigateAction();
    }

    public void CheckAndAction(Action<FrameworkElement> action)
    {
        if (IsChecked == true)
        {
            if (HostContainer?.FindName(TargetFrameControlName) is Frame { Content: FrameworkElement content } frame)
            {
                action.Invoke(content);
            }
        }
        else
        {
            _loadedAction = action;
            IsChecked = true;
        }
    }

    public void SetSingletonPage(Page page)
    {
        var type = page.GetType();
        if (type != TargetPageType)
            throw new NotSupportedException(type.AssemblyQualifiedName + " != " +
                                            TargetPageType.AssemblyQualifiedName);
        _pageInstance = page;

        if (IsChecked == true)
        {
            NavigateAction();
        }
    }

    private void NavigateAction()
    {
        var frame = GetFrameControl();
        if (frame == null) return;

        InnerNavigate(frame);
    }

    private Frame? GetFrameControl()
    {
        if (TargetFrameControl != null)
            return TargetFrameControl;
        if (HostContainer == null)
            return null;
        if (string.IsNullOrWhiteSpace(TargetFrameControlName))
            return null;
        if (HostContainer.FindName(TargetFrameControlName) is Frame frame)
            return frame;
        return null;
    }

    private void InnerNavigate(Frame frame)
    {
        var sw = Stopwatch.StartNew();
        Page page;
        if (TargetPageSingleton && _pageInstance != null)
        {
            page = _pageInstance;
        }
        else
        {
            LogTo.Debug("Creating page instance...");
            page = (Page)(TargetPageData == null
                ? Activator.CreateInstance(TargetPageType)!
                : Activator.CreateInstance(TargetPageType, TargetPageData))!;
            if (TargetPageSingleton)
            {
                _pageInstance = page;
            }

            LogTo.Debug(() => $"Page creation elapsed by {sw.ElapsedMilliseconds}ms");
        }

        LogTo.Debug("Navigating...");
        sw.Restart();
        if (frame is AnimatedFrame animatedFrame)
        {
            animatedFrame.AnimateNavigate(page);
        }
        else
        {
            frame.Navigate(page);
        }

        LogTo.Debug(() => $"Navigation elapsed by {sw.ElapsedMilliseconds}ms");
        sw.Stop();
    }

    public object? TargetPageData
    {
        get => GetValue(TargetPageDataProperty);
        set => SetValue(TargetPageDataProperty, value);
    }

    public static readonly DependencyProperty TargetPageDataProperty =
        DependencyProperty.Register(
            nameof(TargetPageData),
            typeof(object),
            typeof(SwitchRadio)
        );

    public string Scope
    {
        get => (string)GetValue(ScopeProperty);
        set => SetValue(ScopeProperty, value);
    }

    public static readonly DependencyProperty ScopeProperty =
        DependencyProperty.Register(
            nameof(Scope),
            typeof(string),
            typeof(SwitchRadio),
            new PropertyMetadata(null, OnScopeChanged)
        );

    private static void OnScopeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var sr = (SwitchRadio)d;
        var newVal = (string)e.NewValue;
        sr.GroupName = newVal;
    }

    public ControlTemplate IconTemplate
    {
        get => (ControlTemplate)GetValue(IconTemplateProperty);
        set => SetValue(IconTemplateProperty, value);
    }

    public static readonly DependencyProperty IconTemplateProperty =
        DependencyProperty.Register(
            nameof(IconTemplate),
            typeof(ControlTemplate),
            typeof(SwitchRadio),
            null
        );

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(CornerRadius),
            typeof(SwitchRadio),
            new PropertyMetadata(new CornerRadius(0))
        );

    public Thickness IconMargin
    {
        get => (Thickness)GetValue(IconMarginProperty);
        set => SetValue(IconMarginProperty, value);
    }

    public static readonly DependencyProperty IconMarginProperty =
        DependencyProperty.Register(
            nameof(IconMargin),
            typeof(Thickness),
            typeof(SwitchRadio),
            new PropertyMetadata(new Thickness(0, 0, 8, 0))
        );

    public Orientation IconOrientation
    {
        get => (Orientation)GetValue(IconOrientationProperty);
        set => SetValue(IconOrientationProperty, value);
    }

    public static readonly DependencyProperty IconOrientationProperty =
        DependencyProperty.Register(
            nameof(IconOrientation),
            typeof(Orientation),
            typeof(SwitchRadio),
            new PropertyMetadata(Orientation.Horizontal)
        );

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(
            nameof(IconSize),
            typeof(double),
            typeof(SwitchRadio),
            new PropertyMetadata(24d)
        );

    public Brush IconColor
    {
        get => (Brush)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public static readonly DependencyProperty IconColorProperty =
        DependencyProperty.Register(
            nameof(IconColor),
            typeof(Brush),
            typeof(SwitchRadio),
            new PropertyMetadata(null)
        );

    public Type TargetPageType
    {
        get => (Type)GetValue(TargetPageTypeProperty);
        set => SetValue(TargetPageTypeProperty, value);
    }

    public static readonly DependencyProperty TargetPageTypeProperty =
        DependencyProperty.Register(
            nameof(TargetPageType),
            typeof(Type),
            typeof(SwitchRadio)
        );

    public string TargetFrameControlName
    {
        get => (string)GetValue(TargetFrameControlNameProperty);
        set => SetValue(TargetFrameControlNameProperty, value);
    }

    public static readonly DependencyProperty TargetFrameControlNameProperty =
        DependencyProperty.Register(
            nameof(TargetFrameControlName),
            typeof(string),
            typeof(SwitchRadio)
        );

    public Frame? TargetFrameControl
    {
        get => (Frame?)GetValue(TargetFrameControlProperty);
        set => SetValue(TargetFrameControlProperty, value);
    }

    public static readonly DependencyProperty TargetFrameControlProperty =
        DependencyProperty.Register(
            nameof(TargetFrameControl),
            typeof(Frame),
            typeof(SwitchRadio)
        );

    public bool TargetPageSingleton
    {
        get => (bool)GetValue(TargetPageSingletonProperty);
        set => SetValue(TargetPageSingletonProperty, value);
    }

    public static readonly DependencyProperty TargetPageSingletonProperty =
        DependencyProperty.Register(
            nameof(TargetPageSingleton),
            typeof(bool),
            typeof(SwitchRadio)
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

    public Brush MouseOverIconBrush
    {
        get => (Brush)GetValue(MouseOverIconBrushProperty);
        set => SetValue(MouseOverIconBrushProperty, value);
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

    public Brush MouseDownIconBrush
    {
        get => (Brush)GetValue(MouseDownIconBrushProperty);
        set => SetValue(MouseDownIconBrushProperty, value);
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

    public Brush CheckedIconBrush
    {
        get => (Brush)GetValue(CheckedIconBrushProperty);
        set => SetValue(CheckedIconBrushProperty, value);
    }

    public Thickness CheckedBorderThickness
    {
        get => (Thickness)GetValue(CheckedBorderThicknessProperty);
        set => SetValue(CheckedBorderThicknessProperty, value);
    }

    public Brush CheckedBorderBrush
    {
        get => (Brush)GetValue(CheckedBorderBrushProperty);
        set => SetValue(CheckedBorderBrushProperty, value);
    }

    public static readonly DependencyProperty MouseOverBackgroundProperty = DependencyProperty.Register(nameof(MouseOverBackground), typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty MouseOverForegroundProperty = DependencyProperty.Register(nameof(MouseOverForeground), typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty MouseDownBackgroundProperty = DependencyProperty.Register(nameof(MouseDownBackground), typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty MouseDownForegroundProperty = DependencyProperty.Register(nameof(MouseDownForeground), typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty MouseDownIconBrushProperty = DependencyProperty.Register(nameof(MouseDownIconBrush), typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty CheckedBackgroundProperty = DependencyProperty.Register(nameof(CheckedBackground), typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty CheckedForegroundProperty = DependencyProperty.Register(nameof(CheckedForeground), typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty CheckedBorderThicknessProperty = DependencyProperty.Register(nameof(CheckedBorderThickness), typeof(Thickness), typeof(SwitchRadio), new PropertyMetadata(default(Thickness)));
    public static readonly DependencyProperty CheckedBorderBrushProperty = DependencyProperty.Register(nameof(CheckedBorderBrush), typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty MouseOverIconBrushProperty = DependencyProperty.Register(nameof(MouseOverIconBrush), typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty CheckedIconBrushProperty = DependencyProperty.Register(nameof(CheckedIconBrush), typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
}