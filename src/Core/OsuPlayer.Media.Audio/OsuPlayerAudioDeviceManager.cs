using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using KeyAsio.Core.Audio;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.SDL2;
using NAudio.Wave;
using NAudio.Wave.Asio;

namespace OsuPlayer.Media.Audio;

public sealed class OsuPlayerAudioDeviceManager : IAudioDeviceManager
{
    private readonly ILogger<OsuPlayerAudioDeviceManager> _logger;
    private readonly MMDeviceEnumerator? _mmDeviceEnumerator;
    private readonly MmNotificationClient? _mmNotificationClient;

    private bool _disposed;
    private Lazy<Task<IReadOnlyList<DeviceDescription>>> _cachedDevices;

    public OsuPlayerAudioDeviceManager(ILogger<OsuPlayerAudioDeviceManager> logger)
    {
        _logger = logger;
        _cachedDevices = CreateLazyDeviceListAsync();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        try
        {
            _mmDeviceEnumerator = new MMDeviceEnumerator();
            _mmNotificationClient = new MmNotificationClient(this);
            _mmDeviceEnumerator.RegisterEndpointNotificationCallback(_mmNotificationClient);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize Windows audio device notifications.");
        }
    }

    public Task<IReadOnlyList<DeviceDescription>> GetCachedAvailableDevicesAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _cachedDevices.Value;
    }

    public void ClearCache()
    {
        if (_disposed)
        {
            return;
        }

        _cachedDevices = CreateLazyDeviceListAsync();
    }

    public (IWavePlayer Player, DeviceDescription ActualDescription) CreateDevice(
        DeviceDescription? description = null,
        SynchronizationContext? context = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        (IWavePlayer Player, DeviceDescription ActualDescription) result = default;
        if (context != null)
        {
            context.Send(_ => result = CreationCore(description), null);
            return result;
        }

        return CreationCore(description);
    }

    private Lazy<Task<IReadOnlyList<DeviceDescription>>> CreateLazyDeviceListAsync()
    {
        return new Lazy<Task<IReadOnlyList<DeviceDescription>>>(
            () => Task.Run<IReadOnlyList<DeviceDescription>>(() => EnumerateAllDevices().ToArray()),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private (IWavePlayer Player, DeviceDescription ActualDescription) CreationCore(DeviceDescription? description)
    {
        description ??= GetDefaultDeviceDescription();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            description.WavePlayerType != WavePlayerType.SDL)
        {
            _logger.LogWarning(
                "Audio backend {WavePlayerType} is not supported on this platform. Falling back to SDL.",
                description.WavePlayerType);
            description = DeviceDescription.SdlDefault;
        }

        if (description.WavePlayerType == WavePlayerType.ASIO)
        {
            var (device, desc) = CreateAsio(description);
            return (device, desc);
        }

        IWavePlayer wavePlayer = description.WavePlayerType switch
        {
            WavePlayerType.DirectSound => CreateDirectSound(description),
            WavePlayerType.WASAPI => CreateWasapi(description),
            WavePlayerType.SDL => CreateSdl(description),
            _ => throw new ArgumentOutOfRangeException(nameof(description), description.WavePlayerType, null)
        };

        return (wavePlayer, description);
    }

    private DeviceDescription GetDefaultDeviceDescription()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return DeviceDescription.SdlDefault;
        }

        try
        {
            return _mmDeviceEnumerator?.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia) == true
                ? DeviceDescription.WasapiDefault
                : DeviceDescription.DirectSoundDefault;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query default Windows audio endpoint. Falling back to DirectSound.");
            return DeviceDescription.DirectSoundDefault;
        }
    }

    private DirectSoundOut CreateDirectSound(DeviceDescription description)
    {
        EnsureWindowsBackend(description);

        return DeviceComparer.Instance.Equals(description, DeviceDescription.DirectSoundDefault)
            ? new DirectSoundOut(description.Latency)
            : new DirectSoundOut(Guid.Parse(description.DeviceId!), description.Latency);
    }

    private WasapiOut CreateWasapi(DeviceDescription description)
    {
        EnsureWindowsBackend(description);

        if (DeviceComparer.Instance.Equals(description, DeviceDescription.WasapiDefault))
        {
            return CreateDefaultWasapi(description);
        }

        if (_mmDeviceEnumerator == null)
        {
            _logger.LogWarning("WASAPI device enumerator is unavailable. Falling back to default WASAPI device.");
            return CreateDefaultWasapi(description);
        }

        try
        {
            var mmDevice = _mmDeviceEnumerator.GetDevice(description.DeviceId);
            return new WasapiOut(
                mmDevice,
                description.IsExclusive ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared,
                true,
                description.Latency);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while creating WASAPI device {DeviceId}. Falling back to default device.",
                description.DeviceId);
            return CreateDefaultWasapi(description);
        }
    }

    private static WasapiOut CreateDefaultWasapi(DeviceDescription description)
    {
        return new WasapiOut(
            description.IsExclusive ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared,
            description.Latency);
    }

    private static SdlOut CreateSdl(DeviceDescription description)
    {
        var deviceName = string.IsNullOrWhiteSpace(description.DeviceId) ? null : description.DeviceId;
        var bufferFrames = description.Latency > 0 ? description.Latency : 64;
        return new SdlOut(deviceName, bufferFrames);
    }

    private (AsioOut Device, DeviceDescription Description) CreateAsio(DeviceDescription description)
    {
        EnsureWindowsBackend(description);

        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new InvalidOperationException("STA Thread required for ASIO creation.");
        }

        var device = new AsioOut(description.DeviceId);
        var driverExt = device.UnderlineDriver;

        if (description.ForceASIOBufferSize > 0)
        {
            var capability = driverExt.Capabilities;
            capability.BufferPreferredSize = description.ForceASIOBufferSize;

            _logger.LogDebug("Successfully forced ASIO buffer size to {BufferSize}", description.ForceASIOBufferSize);
        }

        var (samples, latency) = GetOutputLatency(driverExt);
        return (device, description with { AsioLatencyMs = latency, AsioActualSamples = samples });
    }

    private static (int Samples, double LatencyMs) GetOutputLatency(AsioDriverExt driverExt)
    {
        double sampleRate = driverExt.Driver.GetSampleRate();
        int outputLatencySamples = driverExt.Capabilities.OutputLatency;
        double outputLatencyMs = outputLatencySamples / sampleRate * 1000.0;
        return (outputLatencySamples, outputLatencyMs);
    }

    private IEnumerable<DeviceDescription> EnumerateAllDevices()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (var deviceDescription in EnumerateFromDirectSound())
            {
                yield return deviceDescription;
            }

            foreach (var deviceDescription in EnumerateFromWasapi())
            {
                yield return deviceDescription;
            }

            foreach (var deviceDescription in EnumerateFromAsio())
            {
                yield return deviceDescription;
            }
        }

        foreach (var deviceDescription in EnumerateFromSdl())
        {
            yield return deviceDescription;
        }
    }

    private IEnumerable<DeviceDescription> EnumerateFromDirectSound()
    {
        IEnumerable<DirectSoundDeviceInfo> devices;
        try
        {
            devices = DirectSoundOut.Devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while enumerating DirectSound devices.");
            devices = [];
        }

        foreach (var dev in devices)
        {
            DeviceDescription? info = null;
            try
            {
                info = new DeviceDescription
                {
                    DeviceId = dev.Guid.ToString(),
                    FriendlyName = dev.Description,
                    WavePlayerType = WavePlayerType.DirectSound
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reading DirectSound device information.");
            }

            if (info != null)
            {
                yield return info;
            }
        }
    }

    private IEnumerable<DeviceDescription> EnumerateFromWasapi()
    {
        yield return DeviceDescription.WasapiDefault;

        if (_mmDeviceEnumerator == null)
        {
            yield break;
        }

        IEnumerable<MMDevice> devices;
        try
        {
            devices = _mmDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while enumerating WASAPI devices.");
            devices = [];
        }

        foreach (var wasapi in devices)
        {
            DeviceDescription? info = null;
            try
            {
                info = new DeviceDescription
                {
                    DeviceId = wasapi.ID,
                    FriendlyName = wasapi.FriendlyName,
                    WavePlayerType = WavePlayerType.WASAPI
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reading WASAPI device information.");
            }

            if (info != null)
            {
                yield return info;
            }
        }
    }

    private IEnumerable<DeviceDescription> EnumerateFromAsio()
    {
        string[] asioDriverNames;
        try
        {
            asioDriverNames = AsioOut.GetDriverNames();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while enumerating ASIO devices.");
            asioDriverNames = [];
        }

        foreach (var asio in asioDriverNames)
        {
            yield return new DeviceDescription
            {
                DeviceId = asio,
                FriendlyName = asio,
                WavePlayerType = WavePlayerType.ASIO
            };
        }
    }

    private IEnumerable<DeviceDescription> EnumerateFromSdl()
    {
        IReadOnlyList<SdlAudioDeviceInfo> devices;
        try
        {
            devices = SdlAudioDevices.GetPlaybackDevices();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while enumerating SDL2 devices.");
            yield break;
        }

        foreach (var sdl in devices)
        {
            yield return sdl.IsDefault
                ? DeviceDescription.SdlDefault
                : new DeviceDescription
                {
                    DeviceId = sdl.Name,
                    FriendlyName = sdl.Name,
                    WavePlayerType = WavePlayerType.SDL
                };
        }
    }

    private static void EnsureWindowsBackend(DeviceDescription description)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                $"{description.WavePlayerType} audio output is only supported on Windows.");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing && _mmDeviceEnumerator != null)
        {
            if (_mmNotificationClient != null)
            {
                try
                {
                    _mmDeviceEnumerator.UnregisterEndpointNotificationCallback(_mmNotificationClient);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to unregister audio device notification callback.");
                }
            }

            _mmDeviceEnumerator.Dispose();
        }

        _disposed = true;
    }

    private sealed class MmNotificationClient(OsuPlayerAudioDeviceManager audioDeviceManager)
        : IMMNotificationClient
    {
        public void OnDeviceStateChanged(string deviceId, DeviceState newState) => audioDeviceManager.ClearCache();

        public void OnDeviceAdded(string pwstrDeviceId) => audioDeviceManager.ClearCache();

        public void OnDeviceRemoved(string deviceId) => audioDeviceManager.ClearCache();

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) =>
            audioDeviceManager.ClearCache();

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
        }
    }
}
