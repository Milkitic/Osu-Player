using System;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapThumb : BaseEntity
    {
        public Guid Id { get; set; }
        public string ThumbPath { get; set; }

        //fk
        public Beatmap Beatmap { get; set; }
        public Guid BeatmapId { get; set; }
    }
}