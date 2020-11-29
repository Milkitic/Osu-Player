using System;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapRecentPlay : BaseEntity
    {
        public Guid Id { get; set; }

        // fk
        public int BeatmapId { get; set; }
        public Beatmap Beatmap { get; set; }
    }
}