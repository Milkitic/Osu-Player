using System.Collections.Generic;

namespace OsuPlayer.Media.Lyric.SourceProvider
{
    public abstract class SongSearchBase<T> where T : SearchSongResultBase, new()
    {
        public abstract List<T> Search(params string[] paramArr);
    }
}
