namespace Milky.OsuPlayer.Common.Configuration
{
    public class GeneralControl
    {
        public bool RunOnStartup { get; set; } = false;
        public string DbPath { get; set; }
        public bool? ExitWhenClosed { get; set; } = null;
    }
}