using NAudio.CoreAudioApi;

namespace OsuPlayer.Devices
{
    public struct WasapiInfo : IDeviceInfo
    {
        public WasapiInfo(string friendlyName, MMDevice device)
        {
            FriendlyName = friendlyName;
            Device = device;
        }

        public OutputMethod OutputMethod => OutputMethod.Wasapi;
        public string FriendlyName { get; private set; }
        public MMDevice Device { get; private set; }

        public static WasapiInfo Default { get; } = new WasapiInfo(null, null);
    }
}