using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayListTest.Models;

namespace PlayListTest
{
    public class PlayList : VmBase
    {
        public PlayList()
        {
            SongList = new ObservableCollection<SongInfo>();
            SongList.CollectionChanged += SongList_CollectionChanged;
            PlayMode = PlayMode.Random;
        }

        public ObservableCollection<SongInfo> SongList { get; set; }

        public SongInfo CurrentInfo
        {
            get => _currentInfo;
            private set
            {
                if (Equals(value, _currentInfo)) return;
                _currentInfo = value;
                OnPropertyChanged();
            }
        }

        public ObservableMixPlayer MixPlayer { get; private set; } = new ObservableMixPlayer();

        public PlayMode PlayMode
        {
            get => _playMode;
            set
            {
                if (Equals(value, _playMode)) return;
                var preIsRandom = IsRandom;
                _playMode = value;
                if (preIsRandom != IsRandom)
                {
                    var b = RearrangeIndexesAndReposition();
                    if (b) throw new Exception("PlayMode changes cause current info changed");
                }

                OnPropertyChanged();
            }
        }

        public async Task SetSongListAsync(ObservableCollection<SongInfo> value, bool startAnew)
        {
            if (Equals(value, SongList)) return;
            if (SongList != null) SongList.CollectionChanged -= SongList_CollectionChanged;
            SongList = value;
            SongList.CollectionChanged += SongList_CollectionChanged;

            var changed = RearrangeIndexesAndReposition(startAnew ? (int?)0 : null);
            if (changed) await SwitchToAsync(null, true);

            OnPropertyChanged(nameof(SongList));
        }

        public async Task<PlayControlResult> SwitchToAsync(PlayControl control)
        {
            return await SwitchToAsync(control == PlayControl.Next, true);
        }

        private Func<int, int> _temporaryPointerChanged;

        private PlayMode _playMode;
        private int _indexPointer = -1;
        private List<int> _songIndexList = new List<int>();
        private SongInfo _currentInfo;
        private ObservableCollection<int> _songIndexList1;

        public ObservableCollection<int> SongIndexList
        {
            get => _songIndexList1;
            set
            {
                if (Equals(value, _songIndexList1)) return;
                _songIndexList1 = value;
                OnPropertyChanged();
            }
        }

        public int IndexPointer
        {
            get => _indexPointer;
            set
            {
                if (value < -1) value = -1;
                else if (value > SongList.Count - 1) value = SongList.Count - 1;

                if (Equals(value, _indexPointer)) return;
                _indexPointer = value;

                if (_indexPointer != -1 && _temporaryPointerChanged != null)
                {
                    _indexPointer = _temporaryPointerChanged.Invoke(_indexPointer);
                    Console.WriteLine(_indexPointer);
                }
                _temporaryPointerChanged = null;

                CurrentInfo = _indexPointer == -1 ? null : SongList[_songIndexList[_indexPointer]];
                OnPropertyChanged();
            }
        }

        private bool IsRandom => _playMode == PlayMode.Random || _playMode == PlayMode.LoopRandom;
        private bool IsLoop => _playMode == PlayMode.Loop || _playMode == PlayMode.LoopRandom;

        internal async Task<PlayControlResult> SwitchToAsync(bool? isNext, bool isManual)
        {
            if (isNext == null)
            {
                if (CurrentInfo == null)
                {
                    IndexPointer = -1;
                    return PlayControlResult.Clear;
                }

                await MixPlayer.LoadAsync(CurrentInfo);
                return PlayControlResult.Success;
            }

            if (!IsLoop && !isManual)
            {
                if (IndexPointer == 0 && isNext == false ||
                    IndexPointer == _songIndexList.Count - 1 && isNext == true)
                {
                    return PlayControlResult.Clear;
                }
            }

            if (PlayMode == PlayMode.Single && !isManual)
            {
                return PlayControlResult.Clear;
            }

            if (PlayMode == PlayMode.SingleLoop && !isManual)
            {
                return PlayControlResult.Keep;
            }

            if (isNext == true)
            {
                if (IndexPointer == _songIndexList.Count - 1 && (isManual || IsLoop))
                    IndexPointer = 0;
                else
                    IndexPointer++;
            }
            else if (isNext == false)
            {
                if (IndexPointer == 0 && (isManual || IsLoop))
                    IndexPointer = _songIndexList.Count - 1;
                else
                    IndexPointer--;
            }

            await MixPlayer.LoadAsync(CurrentInfo);
            return PlayControlResult.Success;
        }

        // returns CurrentInfo changed?
        private bool RearrangeIndexesAndReposition(int? forceIndex = null)
        {
            if (SongList.Count == 0)
            {
                IndexPointer = -1;
                return CurrentInfo != null;
            }

            _songIndexList = SongList.Select((o, i) => i).ToList();
            if (IsRandom) _songIndexList.Shuffle();
            SongIndexList = new ObservableCollection<int>(_songIndexList);

            if (forceIndex != null)
            {
                IndexPointer = forceIndex.Value;
                return true; // force return true
            }

            var indexOf = SongList.IndexOf(CurrentInfo);
            if (indexOf == -1)
            {
                MixPlayer?.Dispose();
                IndexPointer = 0;
                return true;
                //await Player.LoadAsync(CurrentInfo);
            }
            else
            {
                var i = _songIndexList.IndexOf(indexOf);
                IndexPointer = i;
                return false;
            }
        }

        // 如果随机，改变集合是否重排？
        private void SongList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if ((e.NewItems?.Count ?? 0) + (e.OldItems?.Count ?? 0) > 1)
            {
                RearrangeIndexesAndReposition();
                return;
            }

            if (e.NewItems != null && e.NewItems.Count > 0)
            {
                var rnd = new Random();
                if (IsRandom)
                {
                    var index = rnd.Next(IndexPointer + 1, _songIndexList.Count);
                    _songIndexList.Insert(index, e.NewStartingIndex);
                }

                if (e.NewStartingIndex < SongList.Count - 1)
                {
                    for (var i = 0; i < _songIndexList.Count - 1; i++)
                    {
                        if (_songIndexList[i] <= e.NewStartingIndex) _songIndexList[i]++;
                    }
                }

                SongIndexList = new ObservableCollection<int>(_songIndexList);
            }
            else if (e.OldItems != null && e.OldItems.Count > 0)
            {
                var songIndex = e.OldStartingIndex;
                var oldIndexPointer = _songIndexList.IndexOf(songIndex);

                if (oldIndexPointer == IndexPointer)
                {
                    _temporaryPointerChanged = indexPointer =>
                    {
                        if (oldIndexPointer < 0) return indexPointer;
                        _songIndexList.RemoveAt(oldIndexPointer);
                        if (oldIndexPointer < indexPointer) indexPointer--;
                        _temporaryPointerChanged = null;
                        Console.WriteLine(_indexPointer);
                        SongIndexList = new ObservableCollection<int>(_songIndexList);
                        return indexPointer;
                    };
                }
                else
                {
                    _songIndexList.RemoveAt(oldIndexPointer);
                }

                for (var i = 0; i < _songIndexList.Count; i++)
                {
                    if (_songIndexList[i] > songIndex) _songIndexList[i]--;
                }

                SongIndexList = new ObservableCollection<int>(_songIndexList);
            }
        }
    }
}
