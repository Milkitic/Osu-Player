using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Presentation.Interaction;

namespace Milky.OsuPlayer.Services
{
    public class FrameNavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private Frame _frame;

        private static readonly Storyboard FadeinSb;
        private static readonly DoubleAnimation Da1;
        private static readonly DoubleAnimation Ta1;
        private static readonly DoubleAnimation Ta1Clone;

        static FrameNavigationService()
        {
            FadeinSb = new Storyboard { Name = "FadeinSb" };
            Da1 = new DoubleAnimation
            {
                From = 0,
                To = 1,
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
                BeginTime = TimeSpan.Zero,
                Duration = CommonUtils.GetDuration(TimeSpan.FromMilliseconds(300))
            };
            Storyboard.SetTargetProperty(Da1, new PropertyPath(UIElement.OpacityProperty));

            Ta1 = new DoubleAnimation
            {
                From = 0.95,
                To = 1,
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
                BeginTime = TimeSpan.Zero,
                Duration = CommonUtils.GetDuration(TimeSpan.FromMilliseconds(300))
            };
            Ta1Clone = Ta1.Clone();
            Storyboard.SetTargetProperty(Ta1, new PropertyPath("RenderTransform.ScaleX"));
            Storyboard.SetTargetProperty(Ta1Clone, new PropertyPath("RenderTransform.ScaleY"));

            FadeinSb.Children.Add(Da1);
            FadeinSb.Children.Add(Ta1);
            FadeinSb.Children.Add(Ta1Clone);
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

        public void NavigateTo(Type pageType, object parameter = null)
        {
            if (_frame == null)
            {
                throw new InvalidOperationException("NavigationService has not been initialized with a Frame.");
            }

            var page = (FrameworkElement)_serviceProvider.GetRequiredService(pageType);

            if (parameter != null)
            {
                if (page.DataContext is INavigationAware navigationAware)
                {
                    navigationAware.OnNavigatedTo(parameter);
                }
                else if (page is Pages.CollectionPage collectionPage)
                {
                    collectionPage.Id = parameter.ToString();
                }
            }

            var originTransform = page.RenderTransform;
            page.RenderTransformOrigin = new Point(0.5, 0.5);
            Storyboard.SetTarget(Da1, page);
            Storyboard.SetTarget(Ta1, page);
            Storyboard.SetTarget(Ta1Clone, page);

            if (page.RenderTransform.GetType() != typeof(ScaleTransform))
            {
                page.RenderTransform = new ScaleTransform();
            }

            _frame.Navigate(page);

            void OnSbOnCompleted(object sender, EventArgs e)
            {
                page.RenderTransform = originTransform;
                FadeinSb.Completed -= OnSbOnCompleted;
            }

            FadeinSb.Completed += OnSbOnCompleted;
            FadeinSb.Begin();
        }
    }
}
