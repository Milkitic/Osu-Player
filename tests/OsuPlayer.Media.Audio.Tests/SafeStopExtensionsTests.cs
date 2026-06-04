using Milky.OsuPlayer.Media.Audio.Infrastructure;
using NLog;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class SafeStopExtensionsTests
{
    private static readonly Logger NullLogger = LogManager.CreateNullLogger();

    [Fact]
    public async Task TryStopAsync_SwallowsObjectDisposed()
    {
        // The ObjectDisposedException is the expected race when teardown
        // overlaps with an in-flight cancellation; it must not propagate.
        await SafeStopExtensions.TryStopAsync(
            () => throw new ObjectDisposedException("target"),
            NullLogger,
            "test");
    }

    [Fact]
    public async Task TryStopAsync_SwallowsOtherExceptionsAfterLogging()
    {
        // All exceptions are funneled through the logger rather than
        // re-thrown — the coordinator never crashes the audio pipeline
        // because a downstream component failed to teardown cleanly.
        await SafeStopExtensions.TryStopAsync(
            () => throw new InvalidOperationException("boom"),
            NullLogger,
            "test");
    }

    [Fact]
    public async Task TryStopAsync_AwaitsSuccessfulTask()
    {
        var invoked = false;
        await SafeStopExtensions.TryStopAsync(
            () =>
            {
                invoked = true;
                return Task.CompletedTask;
            },
            NullLogger,
            "test");

        Assert.True(invoked);
    }

    [Fact]
    public async Task TryStopAsync_NullArgs_Throw()
    {
        // Argument validation is synchronous. We pass the call through a
        // non-async lambda so the analyzer doesn't flag the obsolete
        // `Assert.Throws<T>(Func<Task>)` overload.
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await SafeStopExtensions.TryStopAsync(null!, NullLogger, "ctx");
        });
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await SafeStopExtensions.TryStopAsync(() => Task.CompletedTask, null!, "ctx");
        });
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await SafeStopExtensions.TryStopAsync(() => Task.CompletedTask, NullLogger, null!);
        });
    }
}
