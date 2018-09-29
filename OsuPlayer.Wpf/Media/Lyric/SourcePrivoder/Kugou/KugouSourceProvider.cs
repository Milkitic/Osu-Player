using Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Base;
using Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Netease;

namespace Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Kugou
{
    public class KugouSourceProvider
        : SourceProviderBase<KugouSearchResultSong, KugouSearcher, KugouLyricDownloader, NeteaseLyricParser>
    {
    }
}
