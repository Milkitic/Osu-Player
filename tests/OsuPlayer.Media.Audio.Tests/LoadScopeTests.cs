using OsuPlayer.Media.Audio.Infrastructure;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class LoadScopeTests
{
    [Fact]
    public async Task AcquireAsync_ReleasesOnDispose()
    {
        var semaphore = new SemaphoreSlim(1, 1);
        var scope = await LoadScope.AcquireAsync(semaphore, CancellationToken.None);
        scope.Dispose();

        // If dispose didn't release, the second acquire would hang.
        var secondAcquire = LoadScope.AcquireAsync(semaphore, CancellationToken.None);
        var completed = await Task.WhenAny(secondAcquire, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.Same(secondAcquire, completed);

        var second = await secondAcquire;
        second.Dispose();

        Assert.Equal(1, semaphore.CurrentCount);
    }

    [Fact]
    public async Task AcquireAsync_HonoursCancellation()
    {
        var semaphore = new SemaphoreSlim(1, 1);
        await semaphore.WaitAsync();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        // SemaphoreSlim surfaces cancellation as the base
        // OperationCanceledException rather than the more specific
        // TaskCanceledException; the contract is "throws OCE".
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await LoadScope.AcquireAsync(semaphore, cts.Token));
    }
}
