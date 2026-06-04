using System;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Media.Audio.Infrastructure;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class CancellableAsyncLoopTests
{
    [Fact]
    public async Task Start_InvokesLoopBodyUntilStopped()
    {
        var loop = new CancellableAsyncLoop();
        int iterations = 0;

        loop.Start(async ct =>
        {
            while (!ct.IsCancellationRequested)
            {
                Interlocked.Increment(ref iterations);
                await Task.Delay(10, ct);
            }
        });

        await Task.Delay(120);
        await loop.StopAsync();

        Assert.True(iterations >= 2, $"Expected at least 2 iterations, got {iterations}");
    }

    [Fact]
    public async Task Start_RepeatedCallsAreIdempotent()
    {
        var loop = new CancellableAsyncLoop();
        int bodyInvocations = 0;

        async Task Body(CancellationToken ct)
        {
            Interlocked.Increment(ref bodyInvocations);
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(10, ct);
            }
        }

        loop.Start(Body);
        loop.Start(Body);
        loop.Start(Body);

        await Task.Delay(50);
        await loop.StopAsync();

        Assert.Equal(1, bodyInvocations);
    }

    [Fact]
    public async Task OnError_IsInvokedForBodyExceptions()
    {
        var loop = new CancellableAsyncLoop();
        Exception? captured = null;
        var signal = new TaskCompletionSource<bool>();

        loop.Start(_ => throw new InvalidOperationException("boom"),
            ex =>
            {
                captured = ex;
                signal.TrySetResult(true);
            });

        await signal.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.NotNull(captured);
        Assert.Equal("boom", captured!.Message);
    }

    [Fact]
    public async Task OperationCanceled_IsSwallowed()
    {
        var loop = new CancellableAsyncLoop();
        bool errorReported = false;

        loop.Start(async ct => await Task.Delay(Timeout.Infinite, ct),
            _ => errorReported = true);

        await Task.Delay(20);
        await loop.StopAsync();

        Assert.False(errorReported);
    }

    [Fact]
    public void Start_NullBody_Throws()
    {
        var loop = new CancellableAsyncLoop();
        Assert.Throws<ArgumentNullException>(() => loop.Start(null!));
    }
}
