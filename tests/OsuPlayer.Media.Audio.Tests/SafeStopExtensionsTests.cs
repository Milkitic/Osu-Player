using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OsuPlayer.Media.Audio.Infrastructure;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class SafeStopExtensionsTests
{
    private static readonly ILogger NullLog = NullLogger.Instance;

    [Fact]
    public async Task TryAsync_SwallowsObjectDisposed()
    {
        await SafeStopExtensions.TryAsync(
            () => throw new ObjectDisposedException("target"),
            NullLog,
            "test");
    }

    [Fact]
    public async Task TryAsync_SwallowsOtherExceptionsAfterLogging()
    {
        await SafeStopExtensions.TryAsync(
            () => throw new InvalidOperationException("boom"),
            NullLog,
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
            NullLog,
            "test");

        Assert.True(invoked);
    }

    [Fact]
    public async Task TryAsync_NullArgs_Throw()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await SafeStopExtensions.TryAsync(null!, NullLog, "ctx");
        });
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await SafeStopExtensions.TryAsync(() => Task.CompletedTask, null!, "ctx");
        });
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await SafeStopExtensions.TryAsync(() => Task.CompletedTask, NullLog, null!);
        });
    }
}
