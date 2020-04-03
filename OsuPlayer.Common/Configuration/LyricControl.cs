using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Media.Lyric;

namespace Milky.OsuPlayer.Common.Configuration
{
    public class LyricControl
    {
        public bool EnableLyric { get; set; } = true;
        public LyricSource LyricSource { get; set; } = LyricSource.Auto;
        public LyricProvideType ProvideType { get; set; } = LyricProvideType.Original;
        public bool StrictMode { get; set; } = true;
        public bool EnableCache { get; set; } = true;
    }
}