namespace Milky.OsuPlayer.Media.Lyric.SourceProvider
{
    public abstract class LyricParserBase
    {
        public abstract Lyric Parse(string content);
    }
}
