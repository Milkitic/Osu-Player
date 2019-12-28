using System;
using Newtonsoft.Json;

namespace OsuPlayer.Devices
{
    public class DirectSoundOutInfo : IDeviceInfo
    {
        public DirectSoundOutInfo()
        {
            
        }

        public DirectSoundOutInfo(string friendlyName,  Guid deviceGuid)
        {
            FriendlyName = friendlyName;
            DeviceGuid = deviceGuid;
        }

        public OutputMethod OutputMethod => OutputMethod.DirectSound;
        [JsonProperty]
        public string FriendlyName { get; private set; }
        [JsonProperty]
        public Guid DeviceGuid { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj is DirectSoundOutInfo deviceInfo)
                return Equals(deviceInfo);
            return false;
        }

        protected bool Equals(DirectSoundOutInfo other)
        {
            return FriendlyName == other.FriendlyName && DeviceGuid.Equals(other.DeviceGuid);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((FriendlyName != null ? FriendlyName.GetHashCode() : 0) * 397) ^ DeviceGuid.GetHashCode();
            }
        }
    }
}