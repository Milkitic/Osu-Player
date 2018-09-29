using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Wpf.Models
{
    public class PlayList
    {
        public List<BeatmapEntry> Entries { get; set; } = new List<BeatmapEntry>();
        public List<int> Indexes { get; set; } = new List<int>();
        public int Pointer { get; set; }
    }
}
