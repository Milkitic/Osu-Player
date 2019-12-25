namespace OsuPlayer.Devices
{
    public class WaveOutInfo : IDeviceInfo
    {
        public WaveOutInfo()
        {

        }

        public WaveOutInfo(string friendlyName, int deviceNumber)
        {
            FriendlyName = friendlyName;
            DeviceNumber = deviceNumber;
        }

        public OutputMethod OutputMethod => OutputMethod.WaveOut;
        public string FriendlyName { get; private set; }
        public int DeviceNumber { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj is WaveOutInfo deviceInfo)
                return Equals(deviceInfo);
            return false;
        }

        protected bool Equals(WaveOutInfo other)
        {
            return FriendlyName == other.FriendlyName && DeviceNumber == other.DeviceNumber;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((FriendlyName != null ? FriendlyName.GetHashCode() : 0) * 397) ^ DeviceNumber;
            }
        }
    }
}