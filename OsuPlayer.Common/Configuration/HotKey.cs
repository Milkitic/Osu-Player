using System;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Milky.OsuPlayer.Common.Configuration
{
    public class HotKey
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public Keys Key { get; set; }
        public bool UseControlKey { get; set; }
        public bool UseAltKey { get; set; }
        public bool UseShiftKey { get; set; }
        [JsonIgnore]
        public Action Callback { get; set; }
    }
}