using Milky.OsuPlayer.Media.Lyric.SourceProvider.Netease;

namespace Milky.OsuPlayer.Media.Lyric.SourceProvider.Kugou
{
    public class KugouSourceProvider
        : SourceProviderBase<KugouSearchResultSong, KugouSearcher, KugouLyricDownloader, NeteaseLyricParser>
    {
        public KugouSourceProvider(bool strictMode) : base(strictMode)
        {
        }
    }
}
