using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Coordination;

internal sealed class SessionOperationManager : IAsyncDisposable
{
    private readonly Lock _gate = new();

    // Retired sources stay alive until all operations drain; in-flight awaits may
    // still register callbacks on tokens captured before a newer operation won.
    private readonly List<CancellationTokenSource> _retiredSources = new();

    private CancellationTokenSource _currentSource = new();
    private TaskCompletionSource<object?> _operationsDrained = CreateCompletedSource();
    private int _activeOperations;
    private bool _stopping;

    public Operation? BeginCurrentOperation()
    {
        lock (_gate)
        {
            if (_stopping) return null;

            TrackActiveOperationLocked();
            return new Operation(this, _currentSource.Token);
        }
    }

    public Operation? BeginInterruptingOperation()
    {
        var next = new CancellationTokenSource();
        CancellationTokenSource previous;

        lock (_gate)
        {
            if (_stopping)
            {
                next.Dispose();
                return null;
            }

            previous = _currentSource;
            _currentSource = next;
            _retiredSources.Add(previous);
            TrackActiveOperationLocked();
        }

        SafeCancel(previous);
        return new Operation(this, next.Token);
    }

    public Task CancelAndDrainAsync()
    {
        CancellationTokenSource source;
        Task drained;

        lock (_gate)
        {
            _stopping = true;
            source = _currentSource;
            drained = _operationsDrained.Task;
        }

        SafeCancel(source);
        return drained;
    }

    public ValueTask DisposeAsync()
    {
        DisposeOperationSources();
        return ValueTask.CompletedTask;
    }

    private void TrackActiveOperationLocked()
    {
        if (_activeOperations == 0)
        {
            _operationsDrained = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        _activeOperations++;
    }

    private void CompleteOperation()
    {
        List<CancellationTokenSource>? sourcesToDispose = null;

        lock (_gate)
        {
            _activeOperations--;
            if (_activeOperations != 0)
            {
                return;
            }

            _operationsDrained.TrySetResult(null);
            if (!_stopping && _retiredSources.Count > 0)
            {
                sourcesToDispose = new List<CancellationTokenSource>(_retiredSources);
                _retiredSources.Clear();
            }
        }

        DisposeSources(sourcesToDispose);
    }

    private void DisposeOperationSources()
    {
        List<CancellationTokenSource> sources;

        lock (_gate)
        {
            sources = new List<CancellationTokenSource>(_retiredSources)
            {
                _currentSource
            };
            _retiredSources.Clear();
        }

        DisposeSources(sources);
    }

    private static void DisposeSources(List<CancellationTokenSource>? sources)
    {
        if (sources == null) return;

        foreach (var source in sources)
        {
            source.Dispose();
        }
    }

    private static void SafeCancel(CancellationTokenSource? cts)
    {
        try
        {
            cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static TaskCompletionSource<object?> CreateCompletedSource()
    {
        var source = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult(null);
        return source;
    }

    public sealed class Operation : IDisposable
    {
        private SessionOperationManager? _owner;

        internal Operation(SessionOperationManager owner, CancellationToken token)
        {
            _owner = owner;
            Token = token;
        }

        public CancellationToken Token { get; }

        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?.CompleteOperation();
        }
    }
}
