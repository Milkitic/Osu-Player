using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Core;
using OsuPlayer.Presentation.Interaction;

namespace OsuPlayer.Services;

public class FrameNavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private Frame _frame;

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
            Duration = CommonUtils.GetDuration(TimeSpan.FromMilliseconds(300))
        };
        Storyboard.SetTargetProperty(s_da1, new PropertyPath(UIElement.OpacityProperty));

        s_ta1 = new DoubleAnimation
        {
            From = 0.95,
            To = 1,
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.Zero,
            Duration = CommonUtils.GetDuration(TimeSpan.FromMilliseconds(300))
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

    public void NavigateTo(Type pageType, object parameter = null, Action<object> pagePrepared = null)
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("NavigationService has not been initialized with a Frame.");
        }

        var page = _serviceProvider.GetRequiredService(pageType);
        pagePrepared?.Invoke(page);

        if (parameter != null)
        {
            if (page is FrameworkElement { DataContext: INavigationAware navigationAware })
            {
                navigationAware.OnNavigatedTo(parameter);
            }
            else if (page is Pages.CollectionPage collectionPage)
            {
                collectionPage.Id = parameter.ToString();
            }
        }

        if (page is FrameworkElement fe)
        {
            var originTransform = fe.RenderTransform;
            fe.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            Storyboard.SetTarget(s_da1, fe);
            Storyboard.SetTarget(s_ta1, fe);
            Storyboard.SetTarget(s_ta1Clone, fe);

            if (fe.RenderTransform.GetType() != typeof(ScaleTransform))
            {
                fe.RenderTransform = new ScaleTransform();
            }

            _frame.Navigate(fe);

            void OnSbOnCompleted(object sender, EventArgs e)
            {
                fe.RenderTransform = originTransform;
                s_fadeinSb.Completed -= OnSbOnCompleted;
            }

            s_fadeinSb.Completed += OnSbOnCompleted;
            s_fadeinSb.Begin();
        }
        else
        {
            _frame.Navigate(page);
        }
    }
}
