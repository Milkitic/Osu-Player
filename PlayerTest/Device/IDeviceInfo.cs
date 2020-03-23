namespace PlayerTest.Device
{
    public interface IDeviceInfo
    {
        OutputMethod OutputMethod { get; }
        string FriendlyName { get; }
        //public override string ToString()
        //{
        //    return $"({OutputMethod}) {FriendlyName}";
        //}
    }
}