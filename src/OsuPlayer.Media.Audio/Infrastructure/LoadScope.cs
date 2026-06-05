using System;
using System.Threading;
using System.Threading.Tasks;

namespace OsuPlayer.Media.Audio.Infrastructure;

/// <summary>
/// RAII guard that releases a <see cref="SemaphoreSlim"/> on dispose.
/// Extracted from the coordinator to be reusable across any serialised
/// load/IO region.
/// </summary>
public readonly struct LoadScope : IDisposable
{
    private readonly SemaphoreSlim _semaphore;

    private LoadScope(SemaphoreSlim semaphore)
    {
        _semaphore = semaphore;
    }

    /// <summary>
    /// Waits asynchronously to enter the supplied semaphore and returns a
    /// scope that releases it on dispose.
    /// </summary>
    public static async Task<LoadScope> AcquireAsync(
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(semaphore);
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new LoadScope(semaphore);
    }

    public void Dispose()
    {
        _semaphore?.Release();
    }
}
