namespace Milky.OsuPlayer.Media.Lyric.SourceProvider
{
    public abstract class LyricDownloaderBase
    {
        public abstract string DownloadLyric(SearchSongResultBase song, bool requestTransLyrics = false);
    }
}
