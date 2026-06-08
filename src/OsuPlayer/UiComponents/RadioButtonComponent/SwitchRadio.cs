using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Presentation;
using OsuPlayer.Presentation.Dependency;
using OsuPlayer.Presentation.Interaction;

namespace OsuPlayer.UiComponents.RadioButtonComponent;

public class SwitchRadio : RadioButton
{
    private static readonly object PendingNavigationDataUnset = new();
    private object _pendingNavigationData = PendingNavigationDataUnset;
    protected FrameworkElement HostWindow { get; private set; }

    public SwitchRadio()
    {
        Loaded += (sender, e) =>
        {
            if (HostWindow != null)
            {
                return;
            }

            //HostWindow = Window.GetWindow(this);
            HostWindow = this.FindParentObjects(typeof(Page), typeof(Window));
        };

        Checked += (sender, e) =>
        {
            if (HostWindow == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(Scope) && Scopes.ContainsKey(Scope))
            {
                var others = Scopes[Scope].Where(k => k != this);
                foreach (var switchRadio in others)
                {
                    switchRadio.IsChecked = false;
                }
            }

            if (string.IsNullOrWhiteSpace(TargetFrameControl))
            {
                return;
            }

            if (HostWindow.FindName(TargetFrameControl) is object frameControl)
            {
                var ui = frameControl is Frame f ? (UIElement)f.Content : null;
                if (ui != null)
                {
                    Storyboard.SetTarget(s_da2, ui);
                    if (AppSettings.Default.Interface.MinimalMode)
                    {
                        OnSbOnCompleted(null, null);
                    }
                    else
                    {
                        s_fadeoutSb.Completed += OnSbOnCompleted;
                    }

                    s_fadeoutSb.Begin();

                    void OnSbOnCompleted(object obj, EventArgs args)
                    {
                        Navigate(frameControl);
                        //ui.BeginAnimation(OpacityProperty, null);
                        //var removeSb = new RemoveStoryboard { BeginStoryboardName = FadeoutSb.Name };
                        s_fadeoutSb.Completed -= OnSbOnCompleted;
                    }
                }
                else
                {
                    Navigate(frameControl);
                }
                //var n = NavigationService.GetNavigationService(frame);
                //frame.NavigationService.Navigate(new Uri($"{TargetPageType}?ExtraData={TargetPageData}", UriKind.Relative), TargetPageData);
            }
        };
    }

    public void NavigateWithData(object targetPageData)
    {
        if (HostWindow == null)
        {
            _pendingNavigationData = targetPageData;
            IsChecked = true;
            return;
        }

        if (IsChecked == true)
        {
            if (HostWindow.FindName(TargetFrameControl) is object frameControl
                && frameControl is Frame frame)
            {
                if (frame.Content is FrameworkElement { DataContext: INavigationAware navigationAware })
                {
                    navigationAware.OnNavigatedTo(targetPageData);
                }
                else
                {
                    _pendingNavigationData = targetPageData;
                    Navigate(frameControl);
                }
            }
        }
        else
        {
            _pendingNavigationData = targetPageData;
            IsChecked = true;
        }
    }

    private void Navigate(object frameControl)
    {
        var targetPageData = ConsumeNavigationData();
        if (App.Services != null)
        {
            var navService = App.Services.GetRequiredService<INavigationService>();
            navService.Initialize(frameControl);

            navService.NavigateTo(TargetPageType, targetPageData);
        }
        else if (frameControl is Frame frame)
        {
            var page = (FrameworkElement)(targetPageData == null
                ? Activator.CreateInstance(TargetPageType)
                : Activator.CreateInstance(TargetPageType, targetPageData));

            if (targetPageData != null)
            {
                if (page.DataContext is INavigationAware navigationAware)
                {
                    navigationAware.OnNavigatedTo(targetPageData);
                }
            }

            var originTransform = page.RenderTransform;
            page.RenderTransformOrigin = new Point(0.5, 0.5);
            Storyboard.SetTarget(s_da1, page);
            Storyboard.SetTarget(s_ta1, page);
            Storyboard.SetTarget(s_ta1Clone, page);
            if (page.RenderTransform.GetType() != typeof(ScaleTransform))
                page.RenderTransform = new ScaleTransform();
            frame.NavigationService.Navigate(page);

            s_fadeinSb.Completed += OnSbOnCompleted;
            s_fadeinSb.Begin();

            void OnSbOnCompleted(object sender, EventArgs e)
            {
                page.RenderTransform = originTransform;
                s_fadeinSb.Completed -= OnSbOnCompleted;
            }
        }
    }

    private object ConsumeNavigationData()
    {
        if (!ReferenceEquals(_pendingNavigationData, PendingNavigationDataUnset))
        {
            var data = _pendingNavigationData;
            _pendingNavigationData = PendingNavigationDataUnset;
            return data;
        }

        return TargetPageData;
    }

    public object TargetPageData
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
        var oldVal = (string)e.OldValue;
        var newVal = (string)e.NewValue;
        var obj = (SwitchRadio)d;
        if (!string.IsNullOrWhiteSpace(oldVal))
        {
            if (Scopes.ContainsKey(oldVal))
            {
                Scopes[oldVal].Remove(obj);
                if (Scopes[oldVal].Count == 0)
                {
                    Scopes.Remove(oldVal);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(newVal))
        {
            if (!Scopes.ContainsKey(newVal))
            {
                Scopes.Add(newVal, new List<SwitchRadio>());
            }

            Scopes[newVal].Add(obj);
        }
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

    public Thickness CornerRadius
    {
        get => (Thickness)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(Thickness),
            typeof(SwitchRadio),
            new PropertyMetadata(new Thickness(0))
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

    public string TargetFrameControl
    {
        get => (string)GetValue(TargetFrameControlProperty);
        set => SetValue(TargetFrameControlProperty, value);
    }

    public static readonly DependencyProperty TargetFrameControlProperty =
        DependencyProperty.Register(
            nameof(TargetFrameControl),
            typeof(string),
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

    public Brush MouseOverIconColor
    {
        get => (Brush)GetValue(MouseOverIconColorProperty);
        set => SetValue(MouseOverIconColorProperty, value);
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

    public Brush MouseDownIconColor
    {
        get => (Brush)GetValue(MouseDownIconColorProperty);
        set => SetValue(MouseDownIconColorProperty, value);
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

    public Brush CheckedIconColor
    {
        get => (Brush)GetValue(CheckedIconColorProperty);
        set => SetValue(CheckedIconColorProperty, value);
    }

    public static readonly DependencyProperty MouseOverBackgroundProperty =
        DependencyProperty.Register(nameof(MouseOverBackground), typeof(Brush), typeof(SwitchRadio),
            new PropertyMetadata(default(Brush)));

    public static readonly DependencyProperty MouseOverForegroundProperty =
        DependencyProperty.Register(nameof(MouseOverForeground), typeof(Brush), typeof(SwitchRadio),
            new PropertyMetadata(default(Brush)));

    public static readonly DependencyProperty MouseDownBackgroundProperty =
        DependencyProperty.Register(nameof(MouseDownBackground), typeof(Brush), typeof(SwitchRadio),
            new PropertyMetadata(default(Brush)));

    public static readonly DependencyProperty MouseDownForegroundProperty =
        DependencyProperty.Register(nameof(MouseDownForeground), typeof(Brush), typeof(SwitchRadio),
            new PropertyMetadata(default(Brush)));

    public static readonly DependencyProperty MouseDownIconColorProperty =
        DependencyProperty.Register(nameof(MouseDownIconColor), typeof(Brush), typeof(SwitchRadio),
            new PropertyMetadata(default(Brush)));

    public static readonly DependencyProperty CheckedBackgroundProperty =
        DependencyProperty.Register(nameof(CheckedBackground), typeof(Brush), typeof(SwitchRadio),
            new PropertyMetadata(default(Brush)));

    public static readonly DependencyProperty CheckedForegroundProperty =
        DependencyProperty.Register(nameof(CheckedForeground), typeof(Brush), typeof(SwitchRadio),
            new PropertyMetadata(default(Brush)));

    public static readonly DependencyProperty MouseOverIconColorProperty =
        DependencyProperty.Register(nameof(MouseOverIconColor), typeof(Brush), typeof(SwitchRadio),
            new PropertyMetadata(default(Brush)));

    public static readonly DependencyProperty CheckedIconColorProperty = DependencyProperty.Register(
        nameof(CheckedIconColor),
        typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));

    private static Dictionary<string, List<SwitchRadio>> Scopes { get; } =
        new Dictionary<string, List<SwitchRadio>>();

    static SwitchRadio()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SwitchRadio),
            new FrameworkPropertyMetadata(typeof(SwitchRadio)));

        s_fadeinSb = new Storyboard { Name = "FadeinSb" };
        s_da1 = new DoubleAnimation
        {
            From = 0,
            To = 1,
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.Zero,
            Duration = AnimationOptions.GetDuration(TimeSpan.FromMilliseconds(300))
        };
        Storyboard.SetTargetProperty(s_da1, new PropertyPath(OpacityProperty));

        s_ta1 = new DoubleAnimation
        {
            From = 0.95,
            To = 1,
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.Zero,
            Duration = AnimationOptions.GetDuration(TimeSpan.FromMilliseconds(300))
        };
        s_ta1Clone = s_ta1.Clone();
        Storyboard.SetTargetProperty(s_ta1, new PropertyPath("RenderTransform.ScaleX"));
        Storyboard.SetTargetProperty(s_ta1Clone, new PropertyPath("RenderTransform.ScaleY"));

        s_fadeinSb.Children.Add(s_da1);
        s_fadeinSb.Children.Add(s_ta1);
        s_fadeinSb.Children.Add(s_ta1Clone);

        s_fadeoutSb = new Storyboard { Name = "FadeoutSb" };
        s_da2 = new DoubleAnimation
        {
            From = 1,
            To = 0,
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.Zero,
            Duration = AnimationOptions.GetDuration(TimeSpan.FromMilliseconds(100))
        };
        s_fadeoutSb.Children.Add(s_da2);
        Storyboard.SetTargetProperty(s_da2, new PropertyPath(OpacityProperty));
    }

    private static readonly DoubleAnimation s_da1;
    private static readonly DoubleAnimation s_ta1;
    private static readonly DoubleAnimation s_ta1Clone;
    private static readonly DoubleAnimation s_da2;
    private static readonly Storyboard s_fadeoutSb;
    private static readonly Storyboard s_fadeinSb;
}
