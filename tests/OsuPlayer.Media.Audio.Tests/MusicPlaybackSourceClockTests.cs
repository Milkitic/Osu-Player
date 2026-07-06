using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;
using OsuPlayer.Media.Audio.SoundTouch;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public sealed class MusicPlaybackSourceClockTests
{
    [Fact]
    public void PlaybackTimelineClock_AdvancesContinuouslyAtCurrentRate()
    {
        long now = 0;
        var clock = new PlaybackTimelineClock(TimeSpan.FromSeconds(10), () => now, 1000);

        clock.Start();
        now += 100;
        Assert.Equal(TimeSpan.FromMilliseconds(100), clock.Position);

        clock.SetRate(0.75f);
        now += 400;
        Assert.Equal(TimeSpan.FromMilliseconds(400), clock.Position);

        clock.SetRate(1.5f);
        now += 200;
        Assert.Equal(TimeSpan.FromMilliseconds(700), clock.Position);
    }

    [Fact]
    public void PlaybackTimelineClock_ClampsOrWrapsAtDuration()
    {
        long now = 0;
        var clock = new PlaybackTimelineClock(TimeSpan.FromMilliseconds(1000), () => now, 1000);

        clock.Start();
        now += 1250;
        Assert.Equal(TimeSpan.FromMilliseconds(1000), clock.Position);

        clock.Seek(TimeSpan.Zero);
        clock.IsLooping = true;
        now += 1250;
        Assert.Equal(TimeSpan.FromMilliseconds(250), clock.Position);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AudioFileMusicPlaybackSource_PositionDoesNotJumpWhenRateProcessorPrefetches(bool preservePitch)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var path = Path.Combine(root, "clock.wav");
        WriteWave(path, frameCount: 44_100 * 5);

        var audioCacheManager = new AudioCacheManager(NullLogger<AudioCacheManager>.Instance);
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44_100, 2);
        var rateProcessorFactory = new SoundTouchPlaybackRateProcessorFactory(readDurationMilliseconds: 250);
        var source = await AudioFileMusicPlaybackSource.CreateAsync(
            audioCacheManager,
            path,
            waveFormat,
            rateProcessorFactory);

        try
        {
            await source.SetPlaybackRateAsync(new PlaybackRateState(0.75f, preservePitch));
            await source.PlayAsync();

            var buffer = new float[waveFormat.SampleRate * waveFormat.Channels];
            var read = source.Output.Read(buffer, 0, buffer.Length);

            Assert.True(read > 0);
            Assert.InRange(source.Position.TotalMilliseconds, 0, 500);
        }
        finally
        {
            await source.DisposeAsync();
            audioCacheManager.ClearAll();
            Directory.Delete(root, recursive: true);
        }
    }

    private static void WriteWave(string path, int frameCount)
    {
        using var writer = new WaveFileWriter(path, new WaveFormat(44_100, 16, 2));
        for (var i = 0; i < frameCount; i++)
        {
            var sample = (float)Math.Sin(i * Math.Tau / 128) * 0.2f;
            writer.WriteSample(sample);
            writer.WriteSample(sample);
        }
    }
}
