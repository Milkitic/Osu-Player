using System;
using Milky.OsuPlayer.Presentation.Interaction;

namespace Milky.OsuPlayer.Media.Audio.Infrastructure;

/// <summary>
/// UI thread marshalling helpers. Centralizes the
/// <c>_uiThreadDispatcher.Send(() =&gt; event?.Invoke(...))</c> pattern that
/// otherwise scatters across coordinator methods.
/// </summary>
public static class RaiseOnUiExtensions
{
    /// <summary>
    /// Posts an action to the UI thread. Equivalent to fire-and-forget
    /// <see cref="IUiThreadDispatcher.Post"/>; swallows no exceptions.
    /// </summary>
    public static void RaiseOnUi(this IUiThreadDispatcher dispatcher, Action action)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(action);
        dispatcher.Post(action);
    }

    /// <summary>
    /// Sends an action to the UI thread synchronously. Prefer
    /// <see cref="RaiseOnUi(System.Action)"/> for event publication to avoid
    /// blocking the calling thread on UI work.
    /// </summary>
    public static void RaiseOnUiSync(this IUiThreadDispatcher dispatcher, Action action)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(action);
        dispatcher.Send(action);
    }

}
