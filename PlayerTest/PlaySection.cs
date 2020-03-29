using Newtonsoft.Json;
using PlayerTest.Device;

namespace PlayerTest
{
    public class PlaySection
    {
        public int GeneralOffset { get; set; }
        [JsonIgnore]
        public int GeneralActualOffset => GeneralOffset /*+ 141*/;
        
        public float PlaybackRate { get; set; } = 1;
        public bool PlayUseTempo { get; set; }
        public IDeviceInfo DeviceInfo { get; set; } = null;
        public int DesiredLatency { get; set; } = 5;
        public bool IsExclusive { get; set; } = false;
    }
}