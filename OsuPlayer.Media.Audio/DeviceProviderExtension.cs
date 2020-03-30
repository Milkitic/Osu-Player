using Milky.OsuPlayer.Common.Configuration;
using NAudio.Wave;
using OsuPlayer.Devices;

namespace Milky.OsuPlayer.Media.Audio
{
    public static class DeviceProviderExtension
    {
        public static IWavePlayer CreateOrGetDefaultDevice()
        {
            var play = AppSettings.Default.Play;
            return DeviceProvider.CreateDevice(play.DeviceInfo, play.DesiredLatency, play.IsExclusive);
        }
    }
}