using System;

namespace PlayListTest
{
    public class ObservablePlaylistPlayer : Player
    {
        public event Action MetaLoaded;
        public event Action BackgroundInfoLoaded;
        public event Action VideoInfoLoaded;
        public event Action StoryboardInfoLoaded;
        public event Action FullLoaded;
        public event Action SongChanged;

        public PlayList PlayList { get; private set; } = new PlayList();

    }
}