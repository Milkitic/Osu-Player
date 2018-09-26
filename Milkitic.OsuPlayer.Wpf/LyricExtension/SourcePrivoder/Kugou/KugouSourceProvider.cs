using Milkitic.OsuPlayer.Wpf.LyricExtension.SourcePrivoder.Base;
using Milkitic.OsuPlayer.Wpf.LyricExtension.SourcePrivoder.Netease;

namespace Milkitic.OsuPlayer.Wpf.LyricExtension.SourcePrivoder.Kugou
{
    public class KugouSourceProvider
        : SourceProviderBase<KugouSearchResultSong, KugouSearcher, KugouLyricDownloader, NeteaseLyricParser>
    {
    }
}
