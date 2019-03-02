using Milky.OsuPlayer.Common.Player;

namespace Milky.OsuPlayer.Common.Configuration
{
    public class PlayControl
    {
        public int GeneralOffset { get; set; } = 25;
        public bool ReplacePlayList { get; set; } = true;
        public bool UsePlayerV2 { get; set; } = false;
        public PlayMod PlayMod { get; set; } = PlayMod.None;
        public bool AutoPlay { get; set; } = false;
        public bool Memory { get; set; } = true;
        public int DesiredLatency { get; set; } = 5;
        public PlayerMode PlayListMode { get; set; } = PlayerMode.Normal;
    }
}