using Milkitic.OsuPlayer.LyricExtension.SourcePrivoder.Base;
using Milkitic.OsuPlayer.LyricExtension.SourcePrivoder.Netease;

namespace Milkitic.OsuPlayer.LyricExtension.SourcePrivoder.Kugou
{
    public class KugouSourceProvider
        : SourceProviderBase<KugouSearchResultSong, KugouSearcher, KugouLyricDownloader, NeteaseLyricParser>
    {
    }
}
