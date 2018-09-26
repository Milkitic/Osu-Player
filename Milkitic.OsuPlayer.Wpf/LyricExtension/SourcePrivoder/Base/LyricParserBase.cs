using Milkitic.OsuPlayer.Wpf.LyricExtension.Model;

namespace Milkitic.OsuPlayer.Wpf.LyricExtension.SourcePrivoder.Base
{
    public abstract class LyricParserBase
    {
        public abstract Lyric Parse(string content);
    }
}
