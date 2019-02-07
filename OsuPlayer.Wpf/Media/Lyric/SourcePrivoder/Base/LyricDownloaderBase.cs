namespace Milky.OsuPlayer.Media.Lyric.SourcePrivoder.Base
{
    public abstract class LyricDownloaderBase
    {
        public abstract string DownloadLyric(SearchSongResultBase song, bool requestTransLyrics = false);
    }
}
