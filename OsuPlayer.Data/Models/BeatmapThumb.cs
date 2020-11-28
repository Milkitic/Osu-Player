using System;
using Milky.OsuPlayer.Presentation.Annotations;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapThumb : BaseEntity
    {
        public Guid Id { get; set; }
        public string ThumbPath { get; set; }
        public string VideoPath { get; set; }

        //fk
        [CanBeNull] public BeatmapStoryboard BeatmapStoryboard { get; set; }
        public Guid? BeatmapStoryboardId { get; set; }
        public Beatmap Beatmap { get; set; }
        public Guid BeatmapId { get; set; }
    }
}