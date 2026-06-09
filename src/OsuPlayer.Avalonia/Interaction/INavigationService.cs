#nullable enable

using System;

namespace OsuPlayer.Avalonia.Interaction;

/// <summary>
/// 统一的导航服务接口，用于解耦 View 与 ViewModel
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// 导航到指定页面类型
    /// </summary>
    void NavigateTo(Type pageType, object? parameter = null);

    /// <summary>
    /// 注册导航的主 Frame (Avalonia 端为 ContentControl)
    /// </summary>
    void Initialize(object frameControl);
}
