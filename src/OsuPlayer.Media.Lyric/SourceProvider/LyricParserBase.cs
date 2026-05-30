using Milky.OsuPlayer.Media.Lyric.Models;

namespace Milky.OsuPlayer.Media.Lyric.SourceProvider
{
    public abstract class LyricParserBase
    {
        public abstract Lyrics Parse(string content);
    }
}
