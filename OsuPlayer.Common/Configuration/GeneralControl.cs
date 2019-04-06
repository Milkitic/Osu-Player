using System.IO;

namespace Milky.OsuPlayer.Common.Configuration
{
    public class GeneralControl
    {
        public bool RunOnStartup { get; set; } = false;
        public string DbPath { get; set; }
        public string CustomSongsPath { get; set; } = Path.Combine(Domain.CurrentPath, "Songs");
        public bool? ExitWhenClosed { get; set; } = null;
        public bool FirstOpen { get; set; } = true;
    }
}