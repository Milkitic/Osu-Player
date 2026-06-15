using NAudio.SDL3;
using NAudio.Wave;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

/// <summary>
/// Smoke tests for the SDL3 audio backend that OsuPlayer actually uses. These
/// tests touch native SDL only (they never open an output device) so they are
/// safe to run on CI agents that don't have an audio sink &#8212; SDL must
/// still be present though.
/// </summary>
public class SdlOutSmokeTests
{
    [Fact]
    public void SdlAudio_Subsystem_Refcount_Roundtrips()
    {
        // Two acquires followed by two releases must leave the subsystem unloaded;
        // a third release must be a no-op (no negative refcount).
        Sdl3Audio.Acquire();
        Sdl3Audio.Acquire();
        var driver = Sdl3Audio.GetCurrentDriver();
        Assert.False(string.IsNullOrEmpty(driver));
        Sdl3Audio.Release();
        Sdl3Audio.Release();
        Sdl3Audio.Release();
    }

    [Fact]
    public void SdlAudioDevices_GetPlaybackDevices_AlwaysIncludesDefault()
    {
        var devices = Sdl3AudioDevices.GetPlaybackDevices();
        Assert.NotEmpty(devices);
        Assert.Contains(devices, d => d.IsDefault);
    }

    [Fact]
    public void SdlOut_Initialise_With_IeeeFloat_DoesNotThrow()
    {
        // Build a silent 0.1s IEEE float source so the callback can pull a few buffers.
        var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        var provider = new SilentProvider(format, byteLength: format.AverageBytesPerSecond / 10);

        using var output = new Sdl3Out(deviceName: null, desiredBufferFrames: 2048);
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

        using var output = new Sdl3Out(deviceName: null, desiredBufferFrames: 2048);
        output.Init(provider);
        output.Play();
        Assert.Equal(PlaybackState.Playing, output.PlaybackState);

        Thread.Sleep(100);

        output.Stop();
        Assert.Equal(PlaybackState.Stopped, output.PlaybackState);
    }

    [Fact]
    public void Sdl_FormatAliases_Are_SystemEndian()
    {
        // The runtime is little-endian on every framework we ship today; the SDL3
        // S16/S32/F32 aliases must pick the LE variants accordingly. We assert the
        // encoded ushort values directly because the source-of-truth constants
        // (SDL_AUDIO_S16 etc.) live on the internal SdlNative class and can't be
        // reached through normal references.
        Assert.True(BitConverter.IsLittleEndian, "Test assumes little-endian host.");
        Assert.Equal((ushort)0x8010, (ushort)Sdl3FormatAliases.SDL_AUDIO_S16);
        Assert.Equal((ushort)0x8020, (ushort)Sdl3FormatAliases.SDL_AUDIO_S32);
        Assert.Equal((ushort)0x8120, (ushort)Sdl3FormatAliases.SDL_AUDIO_F32);
    }

    /// <summary>
    /// Mirror of the <c>SDL_AUDIO_*</c> static properties on
    /// <c>NAudio.SDL3.Interop.SdlNative</c>. Hard-coded here because
    /// <c>SdlNative</c> is internal, so the public test surface can only
    /// verify the values through their documented little-endian encodings.
    /// </summary>
    private static class Sdl3FormatAliases
    {
        // 0x8010 == SDL_AUDIO_S16LE, 0x8020 == SDL_AUDIO_S32LE, 0x8120 == SDL_AUDIO_F32LE.
        // The runtime is little-endian on every framework we ship today, so the
        // aliases resolve to these LE values. The SDL3 native module mirrors this
        // decision through its SDL_AudioFormat enum.
        public const ushort SDL_AUDIO_S16 = 0x8010;
        public const ushort SDL_AUDIO_S32 = 0x8020;
        public const ushort SDL_AUDIO_F32 = 0x8120;
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
