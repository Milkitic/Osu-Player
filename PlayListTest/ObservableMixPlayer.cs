using System;
using System.Threading.Tasks;
using PlayListTest.Models;

namespace PlayListTest
{
    public class ObservableMixPlayer : Player
    {
        public event Action MetaLoaded;
        public event Action BackgroundInfoLoaded;
        public event Action VideoInfoLoaded;
        public event Action StoryboardInfoLoaded;
        public event Action FullLoaded;
        public event Action SongChanged;

        public async Task LoadAsync(SongInfo songInfo)
        {

        }
    }
}