using System;

namespace PlayListTest
{
    public class ObservablePlayer : Player
    {
        public event Action MetaLoaded;
        public event Action BackgroundInfoLoaded;
        public event Action VideoInfoLoaded;
        public event Action StoryboardInfoLoaded;
        public event Action FullLoaded;
        public event Action SongChanged;
    }
}