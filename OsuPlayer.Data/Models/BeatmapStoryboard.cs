using System;
using System.ComponentModel.DataAnnotations;

namespace Milki.OsuPlayer.Data.Models
{
    public class BeatmapStoryboard : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
        public string StoryboardVideoPath { get; set; }

        //fk
        public Beatmap Beatmap { get; set; }
        public byte[] BeatmapId { get; set; }
    }
}