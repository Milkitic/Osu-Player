using KeyAsio.Core.Audio.Caching;
using KeyAsio.Core.OsuAudio.Hitsounds.Playback;
using Microsoft.Extensions.Logging.Abstractions;
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

    [Fact]
    public async Task SetContext_SameBeatmapFilename_DoesNotReusePreviousResource()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var beatmapA = Path.Combine(root, "beatmap-a");
        var beatmapB = Path.Combine(root, "beatmap-b");
        Directory.CreateDirectory(beatmapA);
        Directory.CreateDirectory(beatmapB);

        var audioCacheManager = new AudioCacheManager(NullLogger<AudioCacheManager>.Instance);
        try
        {
            WriteWave(Path.Combine(beatmapA, "normal-hitnormal.wav"), 0.2f);
            WriteWave(Path.Combine(beatmapB, "normal-hitnormal.wav"), 0.8f);

            var cache = new OsuPlaybackEventAudioCache(audioCacheManager);
            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44_100, 2);
            var playbackEvent = PlaybackEvent.Create(
                Guid.NewGuid(),
                0,
                1,
                0,
                "normal-hitnormal.wav",
                ResourceOwner.Beatmap,
                SampleLayer.Primary);

            cache.SetContext(beatmapA, "", "", waveFormat);
            var first = await cache.GetOrCreateAsync(playbackEvent);

            cache.SetContext(beatmapB, "", "", waveFormat);
            var second = await cache.GetOrCreateAsync(playbackEvent);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.NotEqual(first.SourceHash, second.SourceHash);
        }
        finally
        {
            audioCacheManager.ClearAll();
            Directory.Delete(root, recursive: true);
        }
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

    private static void WriteWave(string path, float sample)
    {
        using var writer = new WaveFileWriter(path, new WaveFormat(44_100, 16, 2));
        for (var i = 0; i < 256; i++)
        {
            writer.WriteSample(sample);
            writer.WriteSample(sample);
        }
    }
}
