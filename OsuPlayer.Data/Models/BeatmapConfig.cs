using System;
using System.ComponentModel.DataAnnotations;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapConfig : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
        public int? MainVolume { get; set; }
        public int? MusicVolume { get; set; }
        public int? HitsoundVolume { get; set; }
        public int? SampleVolume { get; set; }
        public int? Offset { get; set; }
        public float? PlaybackRate { get; set; }
        public bool? PlayUseTempo { get; set; }
        public int? LyricOffset { get; set; }
        public string ForceLyricId { get; set; }

        // fk
        public Beatmap Beatmap { get; set; }
        public string BeatmapId { get; set; }
    }
}