using System.Threading;
using Milki.Extensions.MixPlayer.Devices;
using Milky.OsuPlayer.Common.Configuration;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio
{
    public static class DeviceProviderExtension
    {
        public static IWavePlayer CreateOrGetDefaultDevice(out DeviceInfo actualDeviceInfo, SynchronizationContext context)
        {
            var play = AppSettings.Default?.Play;
            if (play != null)
                return DeviceCreationHelper.CreateDevice(out actualDeviceInfo, play.DeviceInfo, context);
            return DeviceCreationHelper.CreateDevice(out actualDeviceInfo, null, context);
        }
    }
}