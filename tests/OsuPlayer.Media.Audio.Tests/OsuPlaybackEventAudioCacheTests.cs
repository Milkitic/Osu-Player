using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KeyAsio.Core.OsuAudio.Hitsounds.Playback;
using Milky.OsuPlayer.Media.Audio;
using NAudio.Wave;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class OsuPlaybackEventAudioCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_ControlEventsWithoutAudio_ReturnsNull()
    {
        var cache = new OsuPlaybackEventAudioCache(null!);

        Assert.Null(await cache.GetOrCreateAsync(
            PlaybackEvent.CreateLoopStopSignal(100, LoopChannel.Normal)));
        Assert.Null(await cache.GetOrCreateAsync(
            PlaybackEvent.CreateLoopVolumeSignal(100, 0.5f)));
        Assert.Null(await cache.GetOrCreateAsync(
            PlaybackEvent.CreateLoopBalanceSignal(100, 0.5f)));
    }

    [Fact]
    public async Task PrecacheRangeAsync_CanceledToken_Throws()
    {
        var cache = new OsuPlaybackEventAudioCache(null!);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            cache.PrecacheRangeAsync(
                new[] { PlaybackEvent.CreateLoopStopSignal(100, LoopChannel.Normal) },
                0,
                1_000,
                cts.Token));
    }

    [Fact]
    public async Task ConcurrentContextChangesAndControlEventCaching_CancelOrComplete()
    {
        var cache = new OsuPlaybackEventAudioCache(null!);
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44_100, 2);
        var events = Enumerable.Range(0, 64)
            .Select(i => PlaybackEvent.CreateLoopStopSignal(i, LoopChannel.Normal))
            .ToArray();

        var tasks = Enumerable.Range(0, Math.Max(2, Environment.ProcessorCount))
            .Select(worker => Task.Run(async () =>
            {
                for (var i = 0; i < 128; i++)
                {
                    cache.SetContext($"beatmap-{worker}-{i}", "", "", waveFormat);
                    await IgnoreContextCancellationAsync(() => cache.PrecacheRangeAsync(events, 0, 1_000));
                    await IgnoreContextCancellationAsync(async () =>
                    {
                        Assert.Null(await cache.GetOrCreateAsync(events[i % events.Length]));
                    });
                }
            }));

        await Task.WhenAll(tasks);
    }

    private static async Task IgnoreContextCancellationAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            // A context change intentionally cancels in-flight cache work.
        }
    }
}
