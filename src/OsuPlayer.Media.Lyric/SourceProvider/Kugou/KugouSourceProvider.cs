namespace Milky.OsuPlayer.Media.Lyric.SourceProvider.Kugou
{
    [SourceProviderName("kugou", "DarkProjector")]
    public class KugouSourceProvider : SourceProviderBase<
        KugouSearchResultSong,
        KugouSearcher,
        KugouLyricDownloader,
        Netease.NeteaseLyricParser>
    {
    }
}
