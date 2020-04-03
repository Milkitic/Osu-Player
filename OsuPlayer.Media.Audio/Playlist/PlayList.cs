using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared.Models;

namespace Milky.OsuPlayer.Media.Audio.Playlist
{
    public class PlayList : VmBase
    {
        public event Action SongListChanged;
        public event Func<PlayControlResult, Beatmap, bool, Task> AutoSwitched;

        public PlayList()
        {
            SongList = new ObservableCollection<Beatmap>();
            SongList.CollectionChanged += SongList_CollectionChanged;
            //PlayerMixer = new ObservablePlayerMixer(this);
        }

        public ObservableCollection<Beatmap> SongList
        {
            get => _songList;
            set
            {
                if (Equals(value, _songList)) return;
                _songList = value;
                SongListChanged?.Invoke();
                OnPropertyChanged();
            }
        }

        public BeatmapContext CurrentInfo
        {
            get => _currentInfo;
            private set
            {
                if (Equals(value, _currentInfo)) return;
                _currentInfo = value;
                OnPropertyChanged();
            }
        }

        public BeatmapContext PreInfo
        {
            get => _preInfo;
            set
            {
                if (Equals(value, _preInfo)) return;
                _preInfo = value;
                OnPropertyChanged();
            }
        }

        public PlaylistMode Mode
        {
            get => _mode;
            set
            {
                if (Equals(value, _mode)) return;
                var preIsRandom = IsRandom;
                _mode = value;
                if (preIsRandom != IsRandom)
                {
                    var b = RearrangeIndexesAndReposition();
                    if (b) throw new Exception("PlayMode changes cause current info changed");
                }

                AppSettings.Default.Play.PlayListMode = _mode;
                AppSettings.SaveDefault();
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

                PreInfo = CurrentInfo;
                CurrentInfo = value == -1 ? null : BeatmapContext.CreateAsync(SongList[_songIndexList[value]]).Result;

                if (Equals(value, _indexPointer)) return;
                _indexPointer = value;

                if (_indexPointer != -1 && _temporaryPointerChanged != null)
                {
                    _indexPointer = _temporaryPointerChanged.Invoke(_indexPointer);
                    Console.WriteLine(_indexPointer);
                }

                _temporaryPointerChanged = null;
                OnPropertyChanged();
            }
        }

        public bool HasCurrent => CurrentInfo != null;

        private bool IsRandom => _mode == PlaylistMode.Random || _mode == PlaylistMode.LoopRandom;
        private bool IsLoop => _mode == PlaylistMode.Loop || _mode == PlaylistMode.LoopRandom;

        private Func<int, int> _temporaryPointerChanged;
        private PlaylistMode _mode;
        private int _indexPointer = -1;
        private List<int> _songIndexList = new List<int>();
        private BeatmapContext _currentInfo;
        private BeatmapContext _preInfo;
        private ObservableCollection<Beatmap> _songList;

        /// <summary>
        /// 播放列表替换
        /// </summary>
        /// <param name="value">播放列表</param>
        /// <param name="startAnew">若为false，则播放列表中若有相同曲，保持指针继续播放</param>
        /// <param name="playInstantly">播放更新列表，并切换当前曲目后，立即播放新的曲目</param>
        /// <param name="autoSetSong">若为false，则仅更改当前列表，不自动切换当前曲目</param>
        /// <returns></returns>
        public async Task<PlayControlResult> SetSongListAsync(IEnumerable<Beatmap> value, bool startAnew,
            bool playInstantly = true, bool autoSetSong = true)
        {
            if (SongList != null) SongList.CollectionChanged -= SongList_CollectionChanged;
            Execute.OnUiThread(() => SongList = new ObservableCollection<Beatmap>(value));
            SongList.CollectionChanged += SongList_CollectionChanged;

            var changed = RearrangeIndexesAndReposition(startAnew ? (int?)0 : null);
            PlayControlResult result; // 这里可能混入空/不空的情况
            if (autoSetSong && changed)
                result = await AutoSwitchAfterCollectionChanged(playInstantly);
            else
                result = new PlayControlResult(PlayControlResult.PlayControlStatus.Keep,
                    PlayControlResult.PointerControlStatus.Keep);

            //OnPropertyChanged(nameof(SongList));
            return result;
        }

        /// <summary>
        /// 播放指定歌曲，若播放列表不存在则自动添加
        /// </summary>
        /// <param name="beatmap"></param>
        /// <returns></returns>
        public void AddOrSwitchTo(Beatmap beatmap)
        {
            if (!SongList.Contains(beatmap))
                Execute.OnUiThread(() => SongList.Add(beatmap));
            IndexPointer = _songIndexList.IndexOf(SongList.IndexOf(beatmap));
        }

        public async Task<PlayControlResult> SwitchByControl(PlayControlType control)
        {
            return await SwitchByControl(control == PlayControlType.Next, true);
        }

        public async Task<PlayControlResult> InvokeAutoNext()
        {
            return await SwitchByControl(true, false);
        }

        private async Task<PlayControlResult> AutoSwitchAfterCollectionChanged(bool playInstantly)
        {
            if (CurrentInfo != null)
            {
                var playControlResult = new PlayControlResult(PlayControlResult.PlayControlStatus.Unknown,
                    PlayControlResult.PointerControlStatus.Default);
                await AutoSwitched?.Invoke(playControlResult, CurrentInfo.Beatmap, playInstantly);
                return playControlResult;
            }

            // 播放列表空
            IndexPointer = -1;
            var controlResult = new PlayControlResult(PlayControlResult.PlayControlStatus.Stop,
                PlayControlResult.PointerControlStatus.Clear);
            await AutoSwitched?.Invoke(controlResult, null, playInstantly);
            return controlResult;
        }

        private async Task<PlayControlResult> SwitchByControl(bool isNext, bool isManual)
        {
            if (!isManual) // auto
            {
                if (Mode == PlaylistMode.Single)
                {
                    var playControlResult = new PlayControlResult(PlayControlResult.PlayControlStatus.Stop,
                        PlayControlResult.PointerControlStatus.Keep);
                    await AutoSwitched?.Invoke(playControlResult, CurrentInfo.Beatmap, true);
                    return playControlResult;
                }

                if (Mode == PlaylistMode.SingleLoop)
                {
                    var playControlResult = new PlayControlResult(PlayControlResult.PlayControlStatus.Play,
                        PlayControlResult.PointerControlStatus.Keep);
                    await AutoSwitched?.Invoke(playControlResult, CurrentInfo.Beatmap, true);
                    return playControlResult;
                }

                if (!IsLoop)
                {
                    if (SongList.Count == 0)
                    {
                        var playControlResult = new PlayControlResult(PlayControlResult.PlayControlStatus.Stop,
                            PlayControlResult.PointerControlStatus.Clear);
                        await AutoSwitched?.Invoke(playControlResult, CurrentInfo.Beatmap, true);
                        return playControlResult;
                    }

                    if (IndexPointer == 0 && !isNext ||
                        IndexPointer == _songIndexList.Count - 1 && isNext)
                    {
                        IndexPointer = 0;
                        var playControlResult = new PlayControlResult(PlayControlResult.PlayControlStatus.Stop,
                            PlayControlResult.PointerControlStatus.Reset);
                        await AutoSwitched?.Invoke(playControlResult, CurrentInfo.Beatmap, true);
                        return playControlResult;
                    }
                }
            }

            if (SongList.Count == 0)
            {
                return new PlayControlResult(PlayControlResult.PlayControlStatus.Stop,
                    PlayControlResult.PointerControlStatus.Clear);
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
                var current = CurrentInfo;
                IndexPointer = -1;
                _songIndexList.Clear();
                return CurrentInfo != current; // 从有到无，则为true
            }

            _songIndexList = SongList.Select((o, i) => i).ToList();
            if (IsRandom) _songIndexList.Shuffle();

            if (forceIndex != null)
            {
                var currentInfo = CurrentInfo;
                IndexPointer = forceIndex.Value;
                return currentInfo != CurrentInfo; // force return false
            }

            var indexOf = SongList.IndexOf(CurrentInfo.Beatmap);
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
            SongListChanged?.Invoke();
            if ((e.NewItems?.Count ?? 0) + (e.OldItems?.Count ?? 0) > 1 || _temporaryPointerChanged != null ||
                SongList.Count == 0)
            {
                _temporaryPointerChanged = null;
                RearrangeIndexesAndReposition();

                if (!CheckCount())
                { }

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
                else
                {
                    _songIndexList.Add(e.NewStartingIndex);
                }

                if (e.NewStartingIndex < SongList.Count - 1)
                {
                    for (var i = 0; i < _songIndexList.Count - 1; i++)
                    {
                        if (_songIndexList[i] <= e.NewStartingIndex) _songIndexList[i]++;
                    }
                }
            }
            else if (e.OldItems != null && e.OldItems.Count > 0)
            {
                var songIndex = e.OldStartingIndex;
                var oldIndexPointer = _songIndexList.IndexOf(songIndex);
                if (oldIndexPointer == -1)
                {
                    _temporaryPointerChanged = null;
                    RearrangeIndexesAndReposition();

                    if (!CheckCount())
                    { }

                    return;
                }

                if (oldIndexPointer == IndexPointer)
                {
                    _temporaryPointerChanged = indexPointer =>
                    {
                        if (oldIndexPointer < 0) return indexPointer;
                        _songIndexList.RemoveAt(oldIndexPointer);
                        if (oldIndexPointer < indexPointer) indexPointer--;
                        _temporaryPointerChanged = null;
                        Console.WriteLine(_indexPointer);
                        return indexPointer;
                    };
                }
                else
                {
                    _temporaryPointerChanged = null;
                    _songIndexList.RemoveAt(oldIndexPointer);
                }

                for (var i = 0; i < _songIndexList.Count; i++)
                {
                    if (_songIndexList[i] > songIndex) _songIndexList[i]--;
                }

                if (!CheckCount())
                { }
            }
        }

        private bool CheckCount()
        {
            return _temporaryPointerChanged != null || SongList.Count == _songIndexList.Count;
        }
    }
}
