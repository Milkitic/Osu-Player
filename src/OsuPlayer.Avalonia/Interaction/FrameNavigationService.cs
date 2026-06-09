#nullable enable

using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Avalonia.AnimationOptions;

namespace OsuPlayer.Avalonia.Interaction;

public sealed class FrameNavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private ContentControl? _content;

    public FrameNavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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

        if (parameter != null && page is Control { DataContext: INavigationAware navigationAware })
        {
            navigationAware.OnNavigatedTo(parameter);
        }

        if (page is Control control)
        {
            NavigateWithAnimation(control);
        }
        else
        {
            _content.Content = page;
        }
    }

    private void NavigateWithAnimation(Control control)
    {
        // 简化: 简单设置内容,使用 Transitions 进行淡入
        _content!.Content = control;
        control.Opacity = 0;

        // 触发一次 Opacity 过渡
        control.Opacity = 1;
    }
}
