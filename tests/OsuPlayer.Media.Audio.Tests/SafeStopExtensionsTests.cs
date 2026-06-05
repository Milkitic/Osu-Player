using Milky.OsuPlayer.Media.Audio.Infrastructure;
using NLog;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class SafeStopExtensionsTests
{
    private static readonly Logger NullLogger = LogManager.CreateNullLogger();

    [Fact]
    public async Task TryAsync_SwallowsObjectDisposed()
    {
        await SafeStopExtensions.TryAsync(
            () => throw new ObjectDisposedException("target"),
            NullLogger,
            "test");
    }

    [Fact]
    public async Task TryAsync_SwallowsOtherExceptionsAfterLogging()
    {
        await SafeStopExtensions.TryAsync(
            () => throw new InvalidOperationException("boom"),
            NullLogger,
            "test");
    }

    [Fact]
    public async Task TryAsync_AwaitsSuccessfulTask()
    {
        var invoked = false;
        await SafeStopExtensions.TryAsync(
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
    public async Task TryAsync_NullArgs_Throw()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await SafeStopExtensions.TryAsync(null!, NullLogger, "ctx");
        });
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await SafeStopExtensions.TryAsync(() => Task.CompletedTask, null!, "ctx");
        });
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await SafeStopExtensions.TryAsync(() => Task.CompletedTask, NullLogger, null!);
        });
    }
}
