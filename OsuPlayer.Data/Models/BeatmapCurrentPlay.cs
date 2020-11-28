using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapCurrentPlay : BaseEntity
    {
        public Guid Id { get; set; }

        // fk
        public Guid BeatmapId { get; set; }
        public Beatmap Beatmap { get; set; }
    }
}
