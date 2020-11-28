using System;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapRecentPlay : BaseEntity
    {
        public Guid Id { get; set; }

        // fk
        public Guid BeatmapId { get; set; }
        public Beatmap Beatmap { get; set; }
    }
}