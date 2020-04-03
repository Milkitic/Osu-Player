using System;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using OSharp.Beatmap;

namespace Milky.OsuPlayer.Common.Player
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
        public Action PlayHandle { get; set; }
        public Action PauseHandle { get; set; }
        public Action StopHandle { get; set; }
        public Action TogglePlayHandle { get; set; }
        public Action<double, bool> SetTimeHandle { get; set; }
        public Action RestartHandle { get; set; }

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