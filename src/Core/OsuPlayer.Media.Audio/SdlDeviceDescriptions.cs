using KeyAsio.Core.Audio;

namespace OsuPlayer.Media.Audio;

internal static class SdlDeviceDescriptions
{
    public static DeviceDescription SdlDefault { get; } = new()
    {
        WavePlayerType = WavePlayerType.SDL,
        FriendlyName = "Default"
    };
}
