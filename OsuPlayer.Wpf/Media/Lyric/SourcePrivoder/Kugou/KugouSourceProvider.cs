using Milky.OsuPlayer.Media.Lyric.SourcePrivoder.Base;
using Milky.OsuPlayer.Media.Lyric.SourcePrivoder.Netease;

namespace Milky.OsuPlayer.Media.Lyric.SourcePrivoder.Kugou
{
    public class KugouSourceProvider
        : SourceProviderBase<KugouSearchResultSong, KugouSearcher, KugouLyricDownloader, NeteaseLyricParser>
    {
        public KugouSourceProvider(bool strictMode) : base(strictMode)
        {
        }
    }
}
