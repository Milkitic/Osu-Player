namespace OsuPlayer.Devices
{
    public struct AsioOutInfo : IDeviceInfo
    {
        public AsioOutInfo(string friendlyName)
        {
            FriendlyName = friendlyName;
        }

        public OutputMethod OutputMethod => OutputMethod.Asio;
        public string FriendlyName { get; private set; }
    }
}