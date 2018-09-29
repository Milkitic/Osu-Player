using System.Collections.Generic;

namespace Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Base
{
    public abstract class SongSearchBase<T> where T : SearchSongResultBase, new()
    {
        public abstract List<T> Search(params string[] paramArr);
    }
}
