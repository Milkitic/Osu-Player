using Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Base;

namespace Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Netease
{
    public class NeteaseSourceProvider : SourceProviderBase<NeteaseSearch.Song, NeteaseSearch, NeteaseLyricDownloader,
        NeteaseLyricParser>
    {
        public NeteaseSourceProvider(bool strictMode) : base(strictMode)
        {
        }
    }
}
