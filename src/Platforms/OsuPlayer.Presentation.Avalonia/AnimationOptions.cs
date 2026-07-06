#nullable enable

using System;

namespace OsuPlayer.Presentation;

public static class AnimationOptions
{
    public static Func<bool>? DisableAnimations { get; set; }

    public static TimeSpan GetDuration(TimeSpan duration)
    {
        return DisableAnimations?.Invoke() == true ? TimeSpan.Zero : duration;
    }
}