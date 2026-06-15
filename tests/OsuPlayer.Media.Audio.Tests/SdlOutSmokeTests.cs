using NAudio.SDL2;
using NAudio.SDL2.Interop;
using NAudio.Wave;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

/// <summary>
/// Smoke tests for the SDL2 audio backend. These tests touch native SDL only
/// (they never open an output device) so they are safe to run on CI agents that
/// don't have an audio sink &#8212; SDL must still be present though.
/// </summary>
public class SdlOutSmokeTests
{
    [Fact]
    public void SdlAudio_Subsystem_Refcount_Roundtrips()
    {
        // Two acquires followed by two releases must leave the subsystem unloaded;
        // a third release must be a no-op (no negative refcount).
        SdlAudio.Acquire();
        SdlAudio.Acquire();
        var driver = SdlAudio.GetCurrentDriver();
        Assert.False(string.IsNullOrEmpty(driver));
        SdlAudio.Release();
        SdlAudio.Release();
        SdlAudio.Release();
    }

    [Fact]
    public void SdlAudioDevices_GetPlaybackDevices_AlwaysIncludesDefault()
    {
        var devices = SdlAudioDevices.GetPlaybackDevices();
        Assert.NotEmpty(devices);
        Assert.Contains(devices, d => d.IsDefault);
    }

    [Fact]
    public void SdlOut_Initialise_With_IeeeFloat_DoesNotThrow()
    {
        // Build a silent 0.1s IEEE float source so the callback can pull a few buffers.
        var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        var provider = new SilentProvider(format, byteLength: format.AverageBytesPerSecond / 10);

        using var output = new SdlOut(deviceName: null, desiredBufferFrames: 2048);
        output.Init(provider);
        Assert.Equal(PlaybackState.Stopped, output.PlaybackState);
        Assert.NotNull(output.OutputWaveFormat);
        Assert.Equal(WaveFormatEncoding.IeeeFloat, output.OutputWaveFormat.Encoding);
    }

    [Fact]
    public void SdlOut_Play_Then_Stop_Transitions_State()
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        var provider = new SilentProvider(format, byteLength: format.AverageBytesPerSecond);

        using var output = new SdlOut(deviceName: null, desiredBufferFrames: 2048);
        output.Init(provider);
        output.Play();
        Assert.Equal(PlaybackState.Playing, output.PlaybackState);

        Thread.Sleep(100);

        output.Stop();
        Assert.Equal(PlaybackState.Stopped, output.PlaybackState);
    }

    [Fact]
    public void SdlOut_Rejects_Unsupported_Format()
    {
        // 24-bit PCM has no direct SDL counterpart and must throw at Init time.
        var format = new WaveFormat(44100, 24, 2);
        var provider = new SilentProvider(format, byteLength: format.AverageBytesPerSecond / 10);

        using var output = new SdlOut(deviceName: null, desiredBufferFrames: 2048);
        Assert.Throws<NotSupportedException>(() => output.Init(provider));
    }

    [Fact]
    public void Sdl_FormatConstants_Are_SystemEndian()
    {
        // The runtime is little-endian on every framework we ship today; the SYS aliases
        // must point at the LSB variants accordingly.
        Assert.True(BitConverter.IsLittleEndian, "Test assumes little-endian host.");
        Assert.Equal(SDL.AUDIO_F32LSB, SDL.AUDIO_F32SYS);
        Assert.Equal(SDL.AUDIO_S16LSB, SDL.AUDIO_S16SYS);
        Assert.Equal(SDL.AUDIO_S32LSB, SDL.AUDIO_S32SYS);
    }

    private sealed class SilentProvider(WaveFormat format, int byteLength) : IWaveProvider
    {
        private int _remaining = byteLength;

        public WaveFormat WaveFormat { get; } = format;

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_remaining <= 0)
            {
                return 0;
            }

            var toWrite = Math.Min(count, _remaining);
            Array.Clear(buffer, offset, toWrite);
            _remaining -= toWrite;
            return toWrite;
        }
    }
}
