using System;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapExport : BaseEntity
    {
        public Guid Id { get; set; }
        public bool IsValid { get; set; }
        public string ExportPath { get; set; }

        //fk
        public Beatmap Beatmap { get; set; }
        public Guid BeatmapId { get; set; }
    }
}