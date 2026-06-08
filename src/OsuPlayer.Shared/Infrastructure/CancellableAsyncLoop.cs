using System;
using System.Threading;
using System.Threading.Tasks;

namespace OsuPlayer.Shared.Infrastructure;

/// <summary>
/// Encapsulates the start/stop lifecycle of an asynchronous loop driven by
/// a <see cref="CancellationTokenSource"/>. Eliminates the repetitive
/// CTS-create / lock-guard / cancel / ObjectDisposedException-swallow pattern.
/// </summary>
public sealed class CancellableAsyncLoop : IDisposable, IAsyncDisposable
{
    private readonly Lock _gate = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;

    /// <summary>
    /// Starts the loop if it is not already running.
    /// </summary>
    /// <param name="loopBody">
    /// The async delegate to run on each iteration. It receives the <see cref="CancellationToken"/>
    /// and must cooperate with cancellation.
    /// </param>
    /// <param name="onError">
    /// Optional error handler invoked when the loop body throws an exception
    /// other than <see cref="OperationCanceledException"/>.
    /// </param>
    public void Start(Func<CancellationToken, Task> loopBody, Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(loopBody);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_loopTask is { IsCompleted: false })
            {
                return;
            }

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _loopTask = Task.Run(() => RunLoopAsync(loopBody, token, onError));
        }
    }

    /// <summary>
    /// Requests cancellation of the running loop. Safe to call multiple times
    /// or when the loop is not running.
    /// </summary>
    public void Stop()
    {
        CancellationTokenSource? cts;
        lock (_gate)
        {
            cts = _cts;
            _cts = null;
        }

        TryCancel(cts);
    }

    /// <summary>
    /// Requests cancellation and waits for the running loop to observe it.
    /// </summary>
    public async ValueTask StopAsync()
    {
        CancellationTokenSource? cts;
        Task? loopTask;
        lock (_gate)
        {
            cts = _cts;
            _cts = null;
            loopTask = _loopTask;
        }

        TryCancel(cts);

        if (loopTask is { IsCompleted: false })
        {
            await loopTask.ConfigureAwait(false);
        }

        cts?.Dispose();
    }

    /// <summary>
    /// Whether the loop is currently running.
    /// </summary>
    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _loopTask is { IsCompleted: false };
            }
        }
    }

    public void Dispose()
    {
        CancellationTokenSource? cts;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            cts = _cts;
            _cts = null;
        }

        TryCancel(cts);

        if (_loopTask is { IsCompleted: true })
        {
            cts?.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        CancellationTokenSource? cts;
        Task? loopTask;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            cts = _cts;
            _cts = null;
            loopTask = _loopTask;
        }

        TryCancel(cts);

        if (loopTask != null)
        {
            await loopTask.ConfigureAwait(false);
        }

        cts?.Dispose();
    }

    private static async Task RunLoopAsync(
        Func<CancellationToken, Task> loopBody,
        CancellationToken cancellationToken,
        Action<Exception>? onError)
    {
        try
        {
            await loopBody(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected cancellation — swallow.
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
    }

    private static void TryCancel(CancellationTokenSource? cts)
    {
        if (cts == null) return;

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Race with Dispose — safe to ignore.
        }
    }
}
