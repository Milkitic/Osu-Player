using System;
using System.Threading.Tasks;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using OSharp.Beatmap;

namespace Milky.OsuPlayer.Media.Audio.Playlist
{
    public class BeatmapContext
    {
        private BeatmapContext(Beatmap beatmap)
        {
            Beatmap = beatmap;
            BeatmapDetail = new BeatmapDetail(beatmap);
        }

        private static AppDbOperator _operator = new AppDbOperator();

        public static async Task<BeatmapContext> CreateAsync(Beatmap beatmap)
        {
            return new BeatmapContext(beatmap)
            {
                BeatmapSettings = _operator.GetMapFromDb(beatmap.GetIdentity()),
            };
        }

        public bool FullLoaded { get; set; } = false;
        public Beatmap Beatmap { get; }
        public BeatmapSettings BeatmapSettings { get; private set; }
        public BeatmapDetail BeatmapDetail { get; }
        public OsuFile OsuFile { get; set; }
        public bool PlayInstantly { get; set; }
        public Func<Task> PlayHandle { get; set; }
        public Func<Task> PauseHandle { get; set; }
        public Func<Task> StopHandle { get; set; }
        public Func<Task> TogglePlayHandle { get; set; }
        public Func<double, bool, Task> SetTimeHandle { get; set; }
        public Func<Task> RestartHandle { get; set; }

        public static bool operator ==(BeatmapContext bc1, BeatmapContext bc2)
        {
            return Equals(bc1, bc2);
        }

        public static bool operator !=(BeatmapContext bc1, BeatmapContext bc2)
        {
            return !(bc1 == bc2);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (!(obj is BeatmapContext bc))
                return false;
            return Equals(bc);
        }

        protected bool Equals(BeatmapContext other)
        {
            return Equals(Beatmap, other.Beatmap);
        }

        public override int GetHashCode()
        {
            return Beatmap != null ? Beatmap.GetHashCode() : 0;
        }
    }
}