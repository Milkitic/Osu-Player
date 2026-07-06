using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public sealed class MusicPlaybackSourceLoopTests
{
    [Fact]
    public async Task AudioFileMusicPlaybackSource_LoopingOutputWrapsAtEnd()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var path = Path.Combine(root, "loop.wav");
        WriteWave(path, sample: 0.2f, frameCount: 4);

        var audioCacheManager = new AudioCacheManager(NullLogger<AudioCacheManager>.Instance);
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44_100, 2);
        var source = await AudioFileMusicPlaybackSource.CreateAsync(audioCacheManager, path, waveFormat);

        try
        {
            var totalSamples = 4 * waveFormat.Channels;
            var nonLoopingBuffer = new float[totalSamples + 4];

            Assert.Equal(totalSamples, source.Output.Read(nonLoopingBuffer, 0, nonLoopingBuffer.Length));

            await source.SeekAsync(TimeSpan.Zero);
            source.IsLooping = true;

            var loopingBuffer = new float[totalSamples + 4];
            Assert.Equal(loopingBuffer.Length, source.Output.Read(loopingBuffer, 0, loopingBuffer.Length));
        }
        finally
        {
            await source.DisposeAsync();
            audioCacheManager.ClearAll();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SilentMusicPlaybackSource_LoopingOutputWrapsAtEnd()
    {
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44_100, 2);
        var source = SilentMusicPlaybackSource.Create(TimeSpan.FromMilliseconds(1), waveFormat);

        try
        {
            var totalSamples = 44 * waveFormat.Channels;
            var nonLoopingBuffer = new float[totalSamples + 4];

            Assert.Equal(totalSamples, source.Output.Read(nonLoopingBuffer, 0, nonLoopingBuffer.Length));

            await source.SeekAsync(TimeSpan.Zero);
            source.IsLooping = true;

            var loopingBuffer = new float[totalSamples + 4];
            Assert.Equal(loopingBuffer.Length, source.Output.Read(loopingBuffer, 0, loopingBuffer.Length));
        }
        finally
        {
            await source.DisposeAsync();
        }
    }

    private static void WriteWave(string path, float sample, int frameCount)
    {
        using var writer = new WaveFileWriter(path, new WaveFormat(44_100, 16, 2));
        for (var i = 0; i < frameCount; i++)
        {
            writer.WriteSample(sample);
            writer.WriteSample(sample);
        }
    }
}
