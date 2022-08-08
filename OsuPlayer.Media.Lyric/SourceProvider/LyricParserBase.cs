using Milki.OsuPlayer.Media.Lyric.Models;

namespace Milki.OsuPlayer.Media.Lyric.SourceProvider
{
    public abstract class LyricParserBase
    {
        public abstract Lyrics Parse(string content);
    }
}
