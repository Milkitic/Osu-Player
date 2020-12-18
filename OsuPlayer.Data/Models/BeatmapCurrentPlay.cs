using System;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapCurrentPlay : BaseEntity
    {
        public Guid Id { get; set; }
        public DateTime PlayTime { get; set; }
        public int Index { get; set; }

        // fk
        public string BeatmapId { get; set; }
        public Beatmap Beatmap { get; set; }
    }
}
