using System;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapStoryboard : BaseEntity
    {
        //[Key]
        public Guid Id { get; set; }
        public string StoryboardVideoPath { get; set; }

        //fk
        public Beatmap Beatmap { get; set; }
        public int BeatmapId { get; set; }
    }
}