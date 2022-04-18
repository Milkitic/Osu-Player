namespace OsuPlayer.Shared;

/// <summary>
/// SemaphoreSlim为核心的lock()异步实现
/// </summary>
public class AsyncLock : IDisposable
{
    private class AsyncLockImpl : IDisposable
    {
        private readonly AsyncLock _parent;
        public AsyncLockImpl(AsyncLock parent) => _parent = parent;
        public void Dispose() => _parent._semaphoreSlim.Release();
    }

    private readonly AsyncLockImpl _asyncLockImpl;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly Task<IDisposable> _completeTask;

    public AsyncLock()
    {
        _semaphoreSlim = new SemaphoreSlim(1, 1);
        _asyncLockImpl = new AsyncLockImpl(this);
        _completeTask = Task.FromResult((IDisposable)_asyncLockImpl);
    }

    public IDisposable Lock()
    {
        _semaphoreSlim.Wait();
        return _asyncLockImpl;
    }

    public Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        var task = _semaphoreSlim.WaitAsync(cancellationToken);
        return task.IsCompleted
            ? _completeTask
            : task.ContinueWith(
                (_, state) => (IDisposable)((AsyncLock)state)._asyncLockImpl,
                this, CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    public void Dispose()
    {
        _semaphoreSlim.Dispose();
    }
}