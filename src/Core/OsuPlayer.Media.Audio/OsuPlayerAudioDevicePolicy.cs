using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using KeyAsio.Core.Audio;
using NAudio.Wave;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Media.Audio;

public static class OsuPlayerAudioDevicePolicy
{
    public const int WasapiFixedLatency = 1;
    public const int SdlDefaultLatency = 1;
    public const bool UseExclusiveMode = false;

    /// <summary>
    /// The latency to persist to settings when the user lets the player choose
    /// </summary>
    public static int RecommendedLatency =>
        DefaultPlayerType == WavePlayerType.WASAPI ? WasapiFixedLatency : SdlDefaultLatency;

    public static WaveFormat DefaultWaveFormat { get; } = new(44100, 2);

    /// <summary>
    /// The wave player type used when no preference is provided. Windows defaults to
    /// WASAPI; everything else defaults to SDL2 because the WASAPI/DirectSound/ASIO
    /// stacks are Windows-only.
    /// </summary>
    public static WavePlayerType DefaultPlayerType { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WavePlayerType.WASAPI : WavePlayerType.SDL;

    /// <summary>
    /// The canonical "auto" device description for the current OS.
    /// </summary>
    public static DeviceDescription DefaultDescription { get; } = DefaultPlayerType switch
    {
        WavePlayerType.WASAPI => DeviceDescription.WasapiDefault,
        WavePlayerType.SDL => DeviceDescription.SdlDefault,
        _ => DeviceDescription.WasapiDefault,
    };

    public static async Task<IReadOnlyList<DeviceDescription>> GetAvailableDevicesAsync(
        IAudioDeviceManager audioDeviceManager)
    {
        var devices = await audioDeviceManager.GetCachedAvailableDevicesAsync().ConfigureAwait(false);
        return devices
            .Where(static device => device.WavePlayerType == WavePlayerType.SDL)
            .Select(Normalize)
            .Distinct(DeviceComparer.Instance)
            .ToArray();
    }

    public static DeviceDescription Normalize(DeviceDescription? deviceDescription)
    {
        // Fall back to the platform default whenever the saved type isn't usable here
        // (e.g. WASAPI-tagged config opened on Linux, or no description at all).
        if (deviceDescription == null || !IsSupportedOnCurrentPlatform(deviceDescription))
        {
            deviceDescription = DefaultDescription;
        }

        return deviceDescription.WavePlayerType switch
        {
            WavePlayerType.WASAPI => deviceDescription with
            {
                Latency = WasapiFixedLatency,
                IsExclusive = UseExclusiveMode,
            },
            WavePlayerType.SDL => deviceDescription with
            {
                Latency = deviceDescription.Latency > 0 ? deviceDescription.Latency : SdlDefaultLatency,
                IsExclusive = false,
            },
            _ => deviceDescription,
        };
    }

    public static DeviceDescription SelectOrDefault(
        IReadOnlyList<DeviceDescription> availableDevices,
        DeviceDescription? preferredDevice)
    {
        var normalizedPreferred = Normalize(preferredDevice);
        return availableDevices.FirstOrDefault(device =>
                   DeviceComparer.Instance.Equals(device, normalizedPreferred))
               ?? availableDevices.FirstOrDefault()
               ?? Normalize(DefaultDescription);
    }

    public static AudioDeviceDescription ToConfiguration(DeviceDescription? deviceDescription)
    {
        var normalized = Normalize(deviceDescription);
        return new AudioDeviceDescription
        {
            WavePlayerType = ToConfigurationType(normalized.WavePlayerType),
            DeviceId = normalized.DeviceId,
            FriendlyName = normalized.FriendlyName,
            Latency = normalized.Latency,
            ForceASIOBufferSize = normalized.ForceASIOBufferSize,
            IsExclusive = normalized.IsExclusive
        };
    }

    public static DeviceDescription? FromConfiguration(AudioDeviceDescription? deviceDescription)
    {
        if (deviceDescription == null)
        {
            return null;
        }

        return new DeviceDescription
        {
            WavePlayerType = ToKeyAsioType(deviceDescription.WavePlayerType),
            DeviceId = deviceDescription.DeviceId,
            FriendlyName = deviceDescription.FriendlyName,
            Latency = deviceDescription.Latency,
            ForceASIOBufferSize = deviceDescription.ForceASIOBufferSize,
            IsExclusive = deviceDescription.IsExclusive
        };
    }

    public static void StartDevice(IPlaybackEngine playbackEngine, AudioDeviceDescription? deviceDescription)
    {
        playbackEngine.StartDevice(Normalize(FromConfiguration(deviceDescription)), DefaultWaveFormat);
    }

    public static void StartDevice(IPlaybackEngine playbackEngine, DeviceDescription? deviceDescription)
    {
        playbackEngine.StartDevice(Normalize(deviceDescription), DefaultWaveFormat);
    }

    /// <summary>
    /// Returns true when the wave player backend for this description can actually be
    /// instantiated on the current operating system. DirectSound/WASAPI/ASIO are
    /// Windows-only; SDL is available everywhere SDL2 is present.
    /// </summary>
    private static bool IsSupportedOnCurrentPlatform(DeviceDescription description)
    {
        return description.WavePlayerType switch
        {
            WavePlayerType.WASAPI or WavePlayerType.DirectSound or WavePlayerType.ASIO =>
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            WavePlayerType.SDL => true,
            _ => false,
        };
    }

    private static AudioOutputType ToConfigurationType(WavePlayerType wavePlayerType)
        => wavePlayerType switch
        {
            WavePlayerType.DirectSound => AudioOutputType.DirectSound,
            WavePlayerType.WASAPI => AudioOutputType.WASAPI,
            WavePlayerType.ASIO => AudioOutputType.ASIO,
            WavePlayerType.SDL => AudioOutputType.SDL,
            _ => AudioOutputType.WASAPI
        };

    private static WavePlayerType ToKeyAsioType(AudioOutputType audioOutputType)
        => audioOutputType switch
        {
            AudioOutputType.DirectSound => WavePlayerType.DirectSound,
            AudioOutputType.WASAPI => WavePlayerType.WASAPI,
            AudioOutputType.ASIO => WavePlayerType.ASIO,
            AudioOutputType.SDL => WavePlayerType.SDL,
            _ => WavePlayerType.WASAPI
        };
}
