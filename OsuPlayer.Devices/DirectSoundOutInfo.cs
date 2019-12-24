using System;

namespace OsuPlayer.Devices
{
    public struct DirectSoundOutInfo : IDeviceInfo
    {
        public DirectSoundOutInfo(string friendlyName,  Guid deviceGuid)
        {
            FriendlyName = friendlyName;
            DeviceGuid = deviceGuid;
        }

        public OutputMethod OutputMethod => OutputMethod.DirectSound;
        public string FriendlyName { get; private set; }
        public Guid DeviceGuid { get; private set; }
    }
}