#nullable enable

namespace OsuPlayer.Shared.Models;

public enum AudioOutputType
{
    DirectSound,
    WASAPI,
    ASIO,
    SDL
}

public sealed class AudioDeviceDescription
{
    public AudioOutputType WavePlayerType { get; set; } = AudioOutputType.WASAPI;

    public string? DeviceId { get; set; }

    public string? FriendlyName { get; set; }

    public int Latency { get; set; }

    public ushort ForceASIOBufferSize { get; set; }

    public bool IsExclusive { get; set; }
}
