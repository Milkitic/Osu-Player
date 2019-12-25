using Newtonsoft.Json;

namespace OsuPlayer.Devices
{
    public class AsioOutInfo : IDeviceInfo
    {
        public AsioOutInfo()
        {

        }

        public AsioOutInfo(string friendlyName)
        {
            FriendlyName = friendlyName;
        }

        public OutputMethod OutputMethod => OutputMethod.Asio;
        [JsonProperty]
        public string FriendlyName { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj is AsioOutInfo deviceInfo)
                return Equals(deviceInfo);
            return false;
        }

        protected bool Equals(AsioOutInfo other)
        {
            return FriendlyName == other.FriendlyName;
        }

        public override int GetHashCode()
        {
            return (FriendlyName != null ? FriendlyName.GetHashCode() : 0);
        }
    }
}