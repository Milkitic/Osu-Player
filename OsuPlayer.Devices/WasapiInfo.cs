using NAudio.CoreAudioApi;
using Newtonsoft.Json;

namespace OsuPlayer.Devices
{
    public class WasapiInfo : IDeviceInfo
    {
        public WasapiInfo()
        {

        }

        public WasapiInfo(string friendlyName, string device)
        {
            FriendlyName = friendlyName;
            DeviceId = device;
            Device = null;
        }

        public OutputMethod OutputMethod => OutputMethod.Wasapi;
        [JsonProperty]
        public string FriendlyName { get; private set; }
        [JsonProperty]
        public string DeviceId { get; private set; }

        [JsonIgnore]
        public MMDevice Device { get; set; }

        public static WasapiInfo Default { get; } = new WasapiInfo(null, null);

        public override bool Equals(object obj)
        {
            if (obj is WasapiInfo deviceInfo)
                return Equals(deviceInfo);
            return false;
        }

        protected bool Equals(WasapiInfo other)
        {
            return FriendlyName == other.FriendlyName && DeviceId == other.DeviceId && Equals(Device, other.Device);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FriendlyName != null ? FriendlyName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DeviceId != null ? DeviceId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Device != null ? Device.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}