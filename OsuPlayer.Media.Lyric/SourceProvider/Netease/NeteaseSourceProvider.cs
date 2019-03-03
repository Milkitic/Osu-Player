namespace Milky.OsuPlayer.Media.Lyric.SourceProvider.Netease
{
    public class NeteaseSourceProvider : SourceProviderBase<NeteaseSearch.Song, NeteaseSearch, NeteaseLyricDownloader,
        NeteaseLyricParser>
    {
        public NeteaseSourceProvider(bool strictMode) : base(strictMode)
        {
        }
    }
}
