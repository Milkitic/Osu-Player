#nullable enable

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OsuPlayer.Presentation.Interaction;

public sealed class FrameNavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private Frame? _frame;

    private static readonly Storyboard s_fadeinSb;
    private static readonly DoubleAnimation s_da1;
    private static readonly DoubleAnimation s_ta1;
    private static readonly DoubleAnimation s_ta1Clone;

    static FrameNavigationService()
    {
        s_fadeinSb = new Storyboard { Name = "FadeinSb" };
        s_da1 = new DoubleAnimation
        {
            From = 0,
            To = 1,
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.Zero,
            Duration = AnimationOptions.GetDuration(TimeSpan.FromMilliseconds(300))
        };
        Storyboard.SetTargetProperty(s_da1, new PropertyPath(UIElement.OpacityProperty));

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
    }

    public FrameNavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize(object frameControl)
    {
        if (frameControl is Frame frame)
        {
            _frame = frame;
        }
    }

    public void NavigateTo(Type pageType, object? parameter = null)
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("NavigationService has not been initialized with a Frame.");
        }

        var page = _serviceProvider.GetService(pageType) ?? Activator.CreateInstance(pageType);
        if (page == null)
        {
            throw new InvalidOperationException($"Unable to create page '{pageType.FullName}'.");
        }

        if (parameter != null && page is FrameworkElement { DataContext: INavigationAware navigationAware })
        {
            navigationAware.OnNavigatedTo(parameter);
        }

        if (page is FrameworkElement frameworkElement)
        {
            NavigateWithAnimation(frameworkElement);
        }
        else
        {
            _frame.Navigate(page);
        }
    }

    private void NavigateWithAnimation(FrameworkElement page)
    {
        var originTransform = page.RenderTransform;
        page.RenderTransformOrigin = new Point(0.5, 0.5);
        Storyboard.SetTarget(s_da1, page);
        Storyboard.SetTarget(s_ta1, page);
        Storyboard.SetTarget(s_ta1Clone, page);

        if (page.RenderTransform.GetType() != typeof(ScaleTransform))
        {
            page.RenderTransform = new ScaleTransform();
        }

        _frame!.Navigate(page);

        void OnCompleted(object? sender, EventArgs e)
        {
            page.RenderTransform = originTransform;
            s_fadeinSb.Completed -= OnCompleted;
        }

        s_fadeinSb.Completed += OnCompleted;
        s_fadeinSb.Begin();
    }
}
