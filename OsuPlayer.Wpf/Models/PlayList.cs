using System.Collections.Generic;
using osu_database_reader.Components.Beatmaps;

namespace Milkitic.OsuPlayer.Models
{
    public class PlayList
    {
        public List<BeatmapEntry> Entries { get; set; } = new List<BeatmapEntry>();
        public List<int> Indexes { get; set; } = new List<int>();
        public int Pointer { get; set; }
    }
}
