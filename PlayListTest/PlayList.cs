using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PlayListTest
{
    public class PlayList : VmBase
    {
        public PlayList()
        {
            SongList = new ObservableCollection<SongInfo>();
            PlayMode = PlayMode.Random;
        }

        public delegate void TemporaryPointerChangedHandler(ref int indexPointer);
        private TemporaryPointerChangedHandler _temporaryPointerChanged;

        private List<int> _songIndexList = new List<int>();
        private int _indexPointer = -1;

        private int IndexPointer
        {
            get => _indexPointer;
            set
            {
                if (Equals(value, _indexPointer)) return;
                _indexPointer = value;

                _temporaryPointerChanged?.Invoke(ref _indexPointer);
                _temporaryPointerChanged = null;

                CurrentInfo = SongList[_songIndexList[_indexPointer]];
            }
        }

        private bool IsRandom => _playMode == PlayMode.Random || _playMode == PlayMode.LoopRandom;
        private bool IsLoop => _playMode == PlayMode.Loop || _playMode == PlayMode.LoopRandom;

        private ObservableCollection<SongInfo> _songList;
        private PlayMode _playMode;

        public ObservableCollection<SongInfo> SongList
        {
            get => _songList;
            set
            {
                if (Equals(value, _songList)) return;
                if (_songList != null) _songList.CollectionChanged -= SongList_CollectionChanged;
                _songList = value;
                _songList.CollectionChanged += SongList_CollectionChanged;

                RearrangeIndexesAndReposition();
                OnPropertyChanged();
            }
        }

        public PlayMode PlayMode
        {
            get => _playMode;
            set
            {
                if (Equals(value, _playMode)) return;
                _playMode = value;

                RearrangeIndexesAndReposition();
                OnPropertyChanged();
            }
        }

        public SongInfo CurrentInfo { get; private set; }

        public SongInfo PlayPrev()
        {

        }

        public SongInfo PlayNext()
        {

        }

        public ObservablePlayer Player { get; private set; } = new ObservablePlayer();

        private void RearrangeIndexesAndReposition()
        {
            _songIndexList = SongList.Select((o, i) => i).ToList();
            if (IsRandom) _songIndexList.Shuffle();
            var indexOf = SongList.IndexOf(CurrentInfo);
            IndexPointer = indexOf == -1 ? 0 : _songIndexList.BinarySearch(indexOf);
        }

        // 如果随机，改变集合是否重排？
        private void SongList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Rearrange
            bool rearrange = false;
            if (e.NewItems.Count == 1)
            {
                var rnd = new Random();
                if (IsRandom) _songIndexList.Insert(rnd.Next(_indexPointer + 1, _songIndexList.Count), e.NewStartingIndex);
                if (e.NewStartingIndex < _songList.Count)
                {
                    for (var i = 0; i < _songIndexList.Count; i++)
                    {
                        if (_songIndexList[i] <= e.NewStartingIndex) _songIndexList[i]++;
                    }
                }
            }
            else if (e.NewItems.Count > 1)
            {
                rearrange = true;
            }

            if (e.OldItems.Count == 1)
            {
                var songIndex = e.OldStartingIndex;
                var oldIndexPointer = _songIndexList.BinarySearch(songIndex);
                for (var i = 0; i < _songIndexList.Count; i++)
                {
                    if (_songIndexList[i] > songIndex) _songIndexList[i]--;
                }

                if (songIndex == IndexPointer)
                {
                    _temporaryPointerChanged = (ref int indexPointer) =>
                    {
                        _songIndexList.RemoveAt(oldIndexPointer);
                        if (oldIndexPointer < indexPointer) indexPointer--;
                    };
                }
            }
            else if (e.NewItems.Count > 1)
            {
                rearrange = true;
            }

            if (rearrange) RearrangeIndexesAndReposition();
        }

    }

    internal static class ListExtension
    {
        private static readonly Random Rnd = new Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Rnd.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
