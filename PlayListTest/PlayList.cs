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
        public event Action<PlayControlResult, SongInfo> AutoSwitched;

        public PlayList()
        {
            SongList = new ObservableCollection<SongInfo>();
            SongList.CollectionChanged += SongList_CollectionChanged;
            PlayMode = PlayMode.Random;
            //PlayerMixer = new ObservablePlayerMixer(this);
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

        //public ObservablePlayerMixer PlayerMixer { get; private set; }

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

        /// <summary>
        /// 播放列表替换
        /// </summary>
        /// <param name="value"></param>
        /// <param name="startAnew">若为true，则播放列表中若有相同曲，保持指针继续播放</param>
        /// <returns></returns>
        public PlayControlResult SetSongList(ObservableCollection<SongInfo> value, bool startAnew)
        {
            if (SongList != null) SongList.CollectionChanged -= SongList_CollectionChanged;
            SongList = value;
            SongList.CollectionChanged += SongList_CollectionChanged;

            var changed = RearrangeIndexesAndReposition(startAnew ? (int?)0 : null);
            var result = changed
                ? AutoSwitchAfterCollectionChanged()
                : new PlayControlResult(PlayControlResult.PlayControlStatus.Keep,
                    PlayControlResult.PointerControlStatus.Keep); // 这里可能混入空/不空的情况

            OnPropertyChanged(nameof(SongList));
            return result;
        }

        /// <summary>
        /// 播放指定歌曲，若播放列表不存在则自动添加
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public void AddOrSwitchTo(SongInfo info)
        {
            if (!SongList.Contains(info)) SongList.Add(info);
            IndexPointer = _songIndexList.IndexOf(SongList.IndexOf(info));
        }

        public PlayControlResult SwitchByControl(PlayControl control)
        {
            return SwitchByControl(control == PlayControl.Next, true);
        }

        public PlayControlResult InvokeAutoNext()
        {
            return SwitchByControl(true, false);
        }

        private PlayControlResult AutoSwitchAfterCollectionChanged()
        {
            if (CurrentInfo != null)
            {
                var playControlResult = new PlayControlResult(PlayControlResult.PlayControlStatus.Unknown,
                    PlayControlResult.PointerControlStatus.Default);
                AutoSwitched?.Invoke(playControlResult, CurrentInfo);
                return playControlResult;
            }

            // 播放列表空
            IndexPointer = -1;
            var controlResult = new PlayControlResult(PlayControlResult.PlayControlStatus.Stop,
                PlayControlResult.PointerControlStatus.Clear);
            AutoSwitched?.Invoke(controlResult, CurrentInfo);
            return controlResult;
        }

        private PlayControlResult SwitchByControl(bool isNext, bool isManual)
        {
            if (!isManual) // auto
            {
                if (PlayMode == PlayMode.Single)
                {
                    var playControlResult = new PlayControlResult(PlayControlResult.PlayControlStatus.Stop,
                        PlayControlResult.PointerControlStatus.Keep);
                    AutoSwitched?.Invoke(playControlResult, CurrentInfo);
                    return playControlResult;
                }

                if (PlayMode == PlayMode.SingleLoop)
                {
                    var playControlResult = new PlayControlResult(PlayControlResult.PlayControlStatus.Play,
                        PlayControlResult.PointerControlStatus.Keep);
                    AutoSwitched?.Invoke(playControlResult, CurrentInfo);
                    return playControlResult;
                }

                if (!IsLoop)
                {
                    if (IndexPointer == 0 && !isNext ||
                        IndexPointer == _songIndexList.Count - 1 && isNext)
                    {
                        var playControlResult = new PlayControlResult(PlayControlResult.PlayControlStatus.Stop,
                            PlayControlResult.PointerControlStatus.Reset);
                        AutoSwitched?.Invoke(playControlResult, CurrentInfo);
                        return playControlResult;
                    }
                }
            }

            if (isNext)
            {
                if (IndexPointer == _songIndexList.Count - 1 && (isManual || IsLoop))
                    IndexPointer = 0;
                else
                    IndexPointer++;
            }
            else
            {
                if (IndexPointer == 0 && (isManual || IsLoop))
                    IndexPointer = _songIndexList.Count - 1;
                else
                    IndexPointer--;
            }

            var result = new PlayControlResult(PlayControlResult.PlayControlStatus.Play,
                PlayControlResult.PointerControlStatus.Default);
            return result;
        }

        // returns CurrentInfo changed?
        private bool RearrangeIndexesAndReposition(int? forceIndex = null)
        {
            if (SongList.Count == 0)
            {
                IndexPointer = -1;
                return CurrentInfo != null; // 从有到无，则为true
            }

            _songIndexList = SongList.Select((o, i) => i).ToList();
            if (IsRandom) _songIndexList.Shuffle();
            SongIndexList = new ObservableCollection<int>(_songIndexList);

            if (forceIndex != null)
            {
                var currentInfo = CurrentInfo;
                IndexPointer = forceIndex.Value;
                return currentInfo != CurrentInfo; // force return false
            }

            var indexOf = SongList.IndexOf(CurrentInfo);
            if (indexOf == -1)
            {
                IndexPointer = 0;
                return true;
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
