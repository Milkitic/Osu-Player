using System;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Infrastructure;

/// <summary>
/// Safe teardown helper for async operations that may race with disposal.
/// Swallows <see cref="ObjectDisposedException"/> (expected during teardown
/// races) and logs all other exceptions through the supplied logger.
/// </summary>
internal static class SafeStopExtensions
{
    public static async Task TryAsync(
        Func<Task> asyncAction,
        NLog.Logger logger,
        string context)
    {
        ArgumentNullException.ThrowIfNull(asyncAction);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await asyncAction().ConfigureAwait(false);
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
