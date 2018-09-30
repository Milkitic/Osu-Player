using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Media;
using Milkitic.OsuPlayer.Utils;
using osu_database_reader.Components.Beatmaps;
using System.Collections.Generic;
using System.Linq;
using Milkitic.OsuPlayer;

namespace Milkitic.OsuPlayer
{
    public class PlayerControl
    {
        private int _pointer;
        public PlayerMode PlayerMode { get; set; } = PlayerMode.Loop;
        public PlayListMode PlayListMode { get; set; }
        public List<BeatmapEntry> Entries { get; set; } = new List<BeatmapEntry>();
        public List<int> Indexes { get; set; } = new List<int>();
        public MapIdentity NowIdentity { get; set; }

        public int Pointer
        {
            get => _pointer;
            set => _pointer = value < 0 ? Indexes.Count - 1 : value;
        }

        /// <summary>
        /// Update current play list.
        /// </summary>
        /// <param name="freshType"></param>
        /// <param name="playListMode">If the value is null, current mode will not be infected.</param>
        /// <param name="entries">If the value is not null, current mode will forcly changed to collection mode.</param>
        public void RefreshPlayList(FreshType freshType, PlayListMode? playListMode = null,
            IEnumerable<BeatmapEntry> entries = null)
        {
            bool force = false;
            if (playListMode != null)
            {
                if (PlayListMode != playListMode.Value)
                    force = true;
                PlayListMode = playListMode.Value;
            }
            if (entries != null)
                PlayListMode = PlayListMode.Collection;
            if (force || entries != null || freshType == FreshType.All || Entries.Count == 0)
                switch (PlayListMode)
                {
                    case PlayListMode.RecentList:
                        Entries = App.Beatmaps.GetRecentListFromDb().ToList();
                        break;
                    default:
                    case PlayListMode.Collection:
                        Entries = entries.ToList();
                        break;
                }

            if (force || entries != null || freshType != FreshType.None || Indexes == null || Indexes.Count == 0)
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
            RedirectPointer();
        }

        public void RedirectPointer()
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].GetIdentity().Equals(NowIdentity))
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

        /// <summary>
        /// Play next song in list if list exist.
        /// </summary>
        /// <param name="isNext">Play next or previous depends on if true or false</param>
        /// <param name="isManual">Whether it is called by user (Click next button manually)
        /// or called by application (A song finshed).</param>
        /// <param name="entry"></param>
        public ChangeType PlayTo(bool isNext, bool isManual, out BeatmapEntry entry)
        {
            if (!isNext)
                Pointer--;
            else
                Pointer++;

            if (Indexes.Count == 0 || Entries.Count == 0)
            {
                entry = null;
                return ChangeType.Stop;
            }

            if (isManual)
            {
                if (Pointer > Indexes.Count - 1)
                {
                    Pointer = 0;
                    RefreshPlayList(FreshType.IndexOnly);
                }
            }
            else
            {
                if (PlayerMode == PlayerMode.Single)
                {
                    entry = null;
                    Pointer--;
                    return ChangeType.Stop;
                }

                if (PlayerMode == PlayerMode.SingleLoop)
                {
                    entry = null;
                    Pointer--;
                    return ChangeType.Keep;
                }

                if (Pointer > Indexes.Count - 1)
                {
                    Pointer = 0;
                    if (PlayerMode == PlayerMode.LoopRandom || PlayerMode == PlayerMode.Loop)
                    {
                        RefreshPlayList(FreshType.IndexOnly);
                    }
                    else
                    {
                        entry = null;
                        return ChangeType.Stop;
                    }
                }
            }

            entry = Entries[Indexes[Pointer]];
            return ChangeType.Change;
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
    }
}
