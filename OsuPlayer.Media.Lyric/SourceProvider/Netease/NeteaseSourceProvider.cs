namespace Milki.OsuPlayer.Media.Lyric.SourceProvider.Netease
{
    [SourceProviderName("netease", "DarkProjector")]
    public class NeteaseSourceProvider : SourceProviderBase<
        NeteaseSearch.Song,
        NeteaseSearch,
        NeteaseLyricDownloader,
        NeteaseLyricParser>
    {

    }
}
