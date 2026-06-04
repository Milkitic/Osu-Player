using System;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Infrastructure;

/// <summary>
/// Safe teardown helpers for objects whose dispose path may race with
/// cancellation. Centralizes the
/// <c>try { await player.Stop() } catch (ObjectDisposedException) { ... }</c>
/// boilerplate that was repeated across the coordinator.
/// </summary>
internal static class SafeStopExtensions
{
    /// <summary>
    /// Awaits <paramref name="stopAsync"/> and swallows
    /// <see cref="ObjectDisposedException"/>, which is the expected race when
    /// teardown overlaps with an in-flight cancellation. Other exceptions are
    /// surfaced through the provided logger.
    /// </summary>
    public static async Task TryStopAsync(
        Func<Task> stopAsync,
        NLog.Logger logger,
        string context)
    {
        ArgumentNullException.ThrowIfNull(stopAsync);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await stopAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException ex)
        {
            logger.Warn(ex, "{Context}: target was already disposed.", context);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{Context}", context);
        }
    }

    /// <summary>
    /// Awaits <paramref name="disposeAsync"/> and swallows
    /// <see cref="ObjectDisposedException"/>. Use when the dispose path also
    /// has non-trivial async work to chain (loop disposal, event unsubscribes,
    /// etc.).
    /// </summary>
    public static async Task TryDisposeAsync(
        Func<Task> disposeAsync,
        NLog.Logger logger,
        string context)
    {
        ArgumentNullException.ThrowIfNull(disposeAsync);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await disposeAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException ex)
        {
            logger.Warn(ex, "{Context}: target was already disposed.", context);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{Context}", context);
        }
    }
}
