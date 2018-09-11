using Milkitic.OsuPlayer.LyricExtension.Model;

namespace Milkitic.OsuPlayer.LyricExtension.SourcePrivoder.Base
{
    public abstract class LyricParserBase
    {
        public abstract Lyric Parse(string content);
    }
}
