using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeyAsio.Core.Audio;
using NAudio.Wave;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Media.Audio;

public static class OsuPlayerAudioDevicePolicy
{
    public const int FixedLatency = 1;
    public const bool UseExclusiveMode = false;

    public static WaveFormat DefaultWaveFormat { get; } = new(44100, 2);

    public static async Task<IReadOnlyList<DeviceDescription>> GetAvailableDevicesAsync(
        IAudioDeviceManager audioDeviceManager)
    {
        var devices = await audioDeviceManager.GetCachedAvailableDevicesAsync().ConfigureAwait(false);
        return devices
            .Where(static device => device.WavePlayerType == WavePlayerType.WASAPI)
            .Select(Normalize)
            .Distinct(DeviceComparer.Instance)
            .ToArray();
    }

    public static DeviceDescription Normalize(DeviceDescription? deviceDescription)
    {
        var wasapiDescription = deviceDescription?.WavePlayerType == WavePlayerType.WASAPI
            ? deviceDescription
            : DeviceDescription.WasapiDefault;

        return wasapiDescription with
        {
            Latency = FixedLatency,
            IsExclusive = UseExclusiveMode
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
               ?? Normalize(DeviceDescription.WasapiDefault);
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

    private static AudioOutputType ToConfigurationType(WavePlayerType wavePlayerType)
        => wavePlayerType switch
        {
            WavePlayerType.DirectSound => AudioOutputType.DirectSound,
            WavePlayerType.WASAPI => AudioOutputType.WASAPI,
            WavePlayerType.ASIO => AudioOutputType.ASIO,
            _ => AudioOutputType.WASAPI
        };

    private static WavePlayerType ToKeyAsioType(AudioOutputType audioOutputType)
        => audioOutputType switch
        {
            AudioOutputType.DirectSound => WavePlayerType.DirectSound,
            AudioOutputType.WASAPI => WavePlayerType.WASAPI,
            AudioOutputType.ASIO => WavePlayerType.ASIO,
            _ => WavePlayerType.WASAPI
        };
}
