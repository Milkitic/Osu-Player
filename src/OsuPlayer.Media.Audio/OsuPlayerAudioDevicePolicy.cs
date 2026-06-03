using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeyAsio.Core.Audio;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio;

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

    public static void StartDevice(IPlaybackEngine playbackEngine, DeviceDescription? deviceDescription)
    {
        playbackEngine.StartDevice(Normalize(deviceDescription), DefaultWaveFormat);
    }
}
