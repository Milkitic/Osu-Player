using OsuPlayer.Media.Lyric.Models;

namespace OsuPlayer.Media.Lyric.SourceProvider;

public abstract class LyricParserBase
{
    public abstract Lyrics Parse(string content);
}