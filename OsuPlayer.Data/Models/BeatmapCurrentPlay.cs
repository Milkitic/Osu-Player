using System;
using System.ComponentModel.DataAnnotations;

namespace Milki.OsuPlayer.Data.Models
{
    public class BeatmapCurrentPlay : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime PlayTime { get; set; }
        public int Index { get; set; }

        // fk
        public byte[] BeatmapId { get; set; }
        public Beatmap Beatmap { get; set; }
    }
}
