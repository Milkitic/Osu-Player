using System;
using KeyAsio.Core.Audio;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace OsuPlayer.Media.Audio;

/// <summary>
/// OsuPlayer's custom <see cref="IPlaybackEngine"/>. Reuses KeyAsio's mixing,
/// volume, and limiter graph, but owns the SDL-specific concerns that used to
/// live inside KeyAsio's <c>AudioEngine</c>:
/// <list type="bullet">
///   <item>Converts the user-facing latency (milliseconds) into SDL buffer frames
///   before the device manager creates the wave player.</item>
///   <item>Restores the user-facing latency on the post-creation description so
///   the engine's <see cref="IPlaybackEngine.CurrentDeviceDescription"/> still
///   reports the milliseconds the user picked.</item>
/// </list>
/// </summary>
public sealed class OsuPlayerAudioEngine : AudioEngine
{
    public OsuPlayerAudioEngine(IAudioDeviceManager audioDeviceManager, ILogger<OsuPlayerAudioEngine>? logger = null)
        : base(audioDeviceManager, logger)
    {
    }

    protected override DeviceDescription? PrepareDeviceDescriptionForCreation(
        DeviceDescription? description,
        WaveFormat waveFormat)
    {
        if (description is not { WavePlayerType: WavePlayerType.SDL })
        {
            return description;
        }

        return description with
        {
            Latency = ConvertLatencyMsToSdlBufferFrames(waveFormat.SampleRate, description.Latency)
        };
    }

    protected override DeviceDescription RestoreDeviceDescriptionForState(
        DeviceDescription? configuredDescription,
        DeviceDescription actualDescription)
    {
        if (configuredDescription?.WavePlayerType == WavePlayerType.SDL &&
            actualDescription.WavePlayerType == configuredDescription.WavePlayerType)
        {
            return actualDescription with { Latency = configuredDescription.Latency };
        }

        return actualDescription;
    }

    private static int ConvertLatencyMsToSdlBufferFrames(int sampleRate, int latencyMs)
    {
        // SDL receives the actual requested buffer size. Keep the ms-to-frame policy
        // here in OsuPlayer where the SDL backend lives.
        var rawFrames = latencyMs <= 0
            ? 64
            : Math.Max(64, (int)((long)sampleRate * latencyMs / 1000));

        var frames = 64;
        while (frames < rawFrames && frames < 4096)
        {
            frames <<= 1;
        }

        return Math.Min(frames, 4096);
    }
}
