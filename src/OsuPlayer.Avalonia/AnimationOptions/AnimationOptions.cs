#nullable enable
using System;

namespace OsuPlayer.Avalonia.AnimationOptions;

/// <summary>
/// 全局动画选项(替代 WPF AnimationOptions)
/// </summary>
public static class AnimationOptionsHelper
{
    public static Func<bool>? DisableAnimations { get; set; }

    public static TimeSpan GetDuration(TimeSpan duration)
    {
        return DisableAnimations?.Invoke() == true ? TimeSpan.Zero : duration;
    }
}
