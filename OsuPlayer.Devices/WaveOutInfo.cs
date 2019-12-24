namespace OsuPlayer.Devices
{
    public struct WaveOutInfo : IDeviceInfo
    {
        public WaveOutInfo(string friendlyName, int deviceNumber)
        {
            FriendlyName = friendlyName;
            DeviceNumber = deviceNumber;
        }

        public OutputMethod OutputMethod => OutputMethod.WaveOut;
        public string FriendlyName { get; private set; }
        public int DeviceNumber { get; private set; }
    }
}