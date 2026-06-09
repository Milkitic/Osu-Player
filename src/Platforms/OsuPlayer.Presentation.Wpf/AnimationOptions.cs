#nullable enable

using System;
using System.Windows;

namespace OsuPlayer.Presentation;

public static class AnimationOptions
{
    public static Func<bool>? DisableAnimations { get; set; }

    public static Duration GetDuration(TimeSpan duration)
    {
        return DisableAnimations?.Invoke() == true
            ? new Duration(TimeSpan.Zero)
            : new Duration(duration);
    }
}
