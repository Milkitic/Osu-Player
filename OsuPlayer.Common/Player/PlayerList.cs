using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Instances;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Common.Player
{
    public class PlayerList : ViewModelBase
    {
        private int _pointer;
        private BeatmapDbOperator _beatmapDbOperator = new BeatmapDbOperator();
        private AppDbOperator _appDbOperator = new AppDbOperator();
        private ObservableCollection<Beatmap> _entries = new ObservableCollection<Beatmap>();
        private CurrentInfo _currentInfo;
        private PlayerMode _playerMode = PlayerMode.Loop;

        public ObservableCollection<Beatmap> Entries
        {
            get => _entries;
            set
            {
                _entries = value;
                OnPropertyChanged();
            }
        }

        public PlayerMode PlayerMode
        {
            get => _playerMode;
            set
            {
                _playerMode = value;
                OnPropertyChanged();
            }
        }

        public PlayListMode PlayListMode { get; set; }
        public List<int> Indexes { get; set; } = new List<int>();

        //public MapIdentity NowIdentity { get; set; }
        public CurrentInfo CurrentInfo
        {
            get => _currentInfo;
            set
            {
                _currentInfo = value;
                OnPropertyChanged();
            }
        }

        public MapIdentity CurrentIdentity => CurrentInfo?.Identity ?? default;
        public int Pointer
        {
            get => _pointer;
            set => _pointer = value < 0 ? (Indexes.Count < 1 ? 1 : Indexes.Count) - 1 : value;
        }

        /// <summary>
        /// Update current play list.
        /// </summary>
        /// <param name="freshType"></param>
        /// <param name="playListMode">If the value is null, current mode will not be infected.</param>
        /// <param name="beatmaps">If the value is not null, current mode will forcly changed to collection mode.</param>
        /// <param name="finishList"></param>
        public async Task RefreshPlayListAsync(
            FreshType freshType,
            PlayListMode? playListMode = null,
            IEnumerable<Beatmap> beatmaps = null, bool finishList = false)
        {
            bool force = false;
            if (playListMode != null)
            {
                if (PlayListMode != playListMode.Value)
                    force = true;
                PlayListMode = playListMode.Value;
            }
            if (beatmaps != null)
                PlayListMode = PlayListMode.Collection;
            if (force || beatmaps != null || freshType == FreshType.All || Entries.Count == 0)
                switch (PlayListMode)
                {
                    case PlayListMode.RecentList:
                        var mapInfos = _appDbOperator.GetRecentList();
                        Entries = new ObservableCollection<Beatmap>(_beatmapDbOperator.GetBeatmapsByMapInfo(mapInfos, TimeSortMode.PlayTime));
                        break;
                    default:
                    case PlayListMode.Collection:
                        if (beatmaps != null)
                            Entries = new ObservableCollection<Beatmap>(await Task.Run(() => beatmaps.ToList())); //todo: 150ms
                        break;
                }

            if (force || beatmaps != null || freshType != FreshType.None || Indexes == null || Indexes.Count == 0)
                switch (PlayerMode)
                {
                    default:
                    case PlayerMode.Normal:
                    case PlayerMode.Loop:
                        Indexes = Entries.Select((o, i) => i).ToList();
                        break;
                    case PlayerMode.Random:
                    case PlayerMode.LoopRandom:
                        Indexes = Entries.Select((o, i) => i).ShuffleToList();
                        break;
                }
            Pointer = 0;
            if (!finishList && CurrentInfo != null) RedirectPointer();

            AppSettings.Default.CurrentList = Entries.Select(k => k.GetIdentity()).ToList();
            AppSettings.SaveDefault();
        }

        /// <summary>
        /// Play next song in list if list exist.
        /// </summary>
        /// <param name="isNext">Play next or previous depends on if true or false</param>
        /// <param name="isManual">Whether it is called by user (Click next button manually)
        /// or called by application (A song finshed).</param>
        public async Task<(ChangeType, Beatmap)> PlayToAsync(bool isNext, bool isManual)
        {
            if (!isNext)
                Pointer--;
            else
                Pointer++;

            if (Indexes.Count == 0 || Entries.Count == 0)
            {
                return (ChangeType.Stop, null);
            }

            if (isManual)
            {
                if (Pointer > Indexes.Count - 1)
                {
                    Pointer = 0;
                    await RefreshPlayListAsync(FreshType.IndexOnly, finishList: true);
                }
            }
            else
            {
                if (PlayerMode == PlayerMode.Single)
                {
                    return (ChangeType.Stop, Entries.First(k => k.GetIdentity().Equals(CurrentIdentity)));
                }

                if (PlayerMode == PlayerMode.SingleLoop)
                {
                    return (ChangeType.Keep, Entries.First(k => k.GetIdentity().Equals(CurrentIdentity)));
                }

                if (Pointer > Indexes.Count - 1)
                {
                    Pointer = 0;
                    if (PlayerMode == PlayerMode.LoopRandom || PlayerMode == PlayerMode.Loop)
                    {
                        await RefreshPlayListAsync(FreshType.IndexOnly, finishList: true);
                    }
                    else
                    {
                        return (ChangeType.Stop, null);
                    }
                }
            }

            return (ChangeType.Change, Entries[Indexes[Pointer]]);
        }

        public enum ChangeType
        {
            Keep, Change, Stop
        }

        public enum FreshType
        {
            /// <summary>
            /// Refresh the whole list including PlayList and IndexList.
            /// </summary>
            All,
            /// <summary>
            /// Refresh only the IndexList.
            /// </summary>
            IndexOnly,
            /// <summary>
            /// Keep all the lists.
            /// </summary>
            None
        }

        private void RedirectPointer()
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].GetIdentity().Equals(CurrentInfo.Identity))
                {
                    for (int j = 0; j < Indexes.Count; j++)
                    {
                        if (Indexes[j] != i)
                            continue;
                        Pointer = j;
                        break;
                    }

                    break;
                }
            }
        }
    }
}
