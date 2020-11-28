using OSharp.Beatmap.MetaData;
using System;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapStoryboard : BaseEntity
    {
        //[Key]
        public Guid Id { get; set; }
        public string SbThumbPath { get; set; }
        public string SbThumbVideoPath { get; set; }

        //fk
        public Beatmap Beatmap { get; set; }
        public Guid BeatmapId { get; set; }
    }
}