#nullable enable

using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace OsuPlayer.Presentation.Interaction;

public sealed class FrameNavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private ContentControl? _content;

    private readonly TimeSpan _animationDuration;

    public FrameNavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _animationDuration = AnimationOptions.GetDuration(TimeSpan.FromMilliseconds(300));
    }

    public void Initialize(object frameControl)
    {
        if (frameControl is ContentControl content)
        {
            _content = content;
        }
    }

    public void NavigateTo(Type pageType, object? parameter = null)
    {
        if (_content == null)
        {
            throw new InvalidOperationException("NavigationService has not been initialized with a ContentControl.");
        }

        var page = _serviceProvider.GetService(pageType) ?? Activator.CreateInstance(pageType);
        if (page == null)
        {
            throw new InvalidOperationException($"Unable to create page '{pageType.FullName}'.");
        }

        if (page is Control control)
        {
            TrySetDataContext(control, pageType);

            if (parameter != null && control.DataContext is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(parameter);
            }

            NavigateWithAnimation(control);
        }
        else
        {
            _content.Content = page;
        }
    }

    private void TrySetDataContext(Control control, Type pageType)
    {
        if (control.DataContext != null) return;

        var vmTypeName = pageType.Namespace + "." + pageType.Name + "ViewModel";
        var vmType = Type.GetType(vmTypeName + ", " + pageType.Assembly.GetName().Name);
        if (vmType != null)
        {
            var vm = _serviceProvider.GetService(vmType);
            if (vm != null)
            {
                control.DataContext = vm;
            }
        }
    }

    private async void NavigateWithAnimation(Control control)
    {
        _content!.Content = control;

        var scale = new ScaleTransform(0.95, 0.95);
        control.SetValue(Control.OpacityProperty, 0d);
        control.RenderTransform = scale;
        control.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

        var animation = new Avalonia.Animation.Animation
        {
            Duration = _animationDuration,
            Easing = new ExponentialEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter(Control.OpacityProperty, 0d),
                        new Setter(ScaleTransform.ScaleXProperty, 0.95),
                        new Setter(ScaleTransform.ScaleYProperty, 0.95),
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter(Control.OpacityProperty, 1d),
                        new Setter(ScaleTransform.ScaleXProperty, 1d),
                        new Setter(ScaleTransform.ScaleYProperty, 1d),
                    }
                }
            }
        };

        await animation.RunAsync(control);

        control.ClearValue(Control.OpacityProperty);
        control.RenderTransform = null;
    }
}
