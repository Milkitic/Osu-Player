namespace Milkitic.OsuPlayer.LyricExtension.SourcePrivoder.Base
{
    public abstract class SearchSongResultBase
    {
        public abstract string Title { get; }
        public abstract string Artist { get; }
        public abstract int Duration { get; }
        public abstract string ResultId { get; }
    }
}
