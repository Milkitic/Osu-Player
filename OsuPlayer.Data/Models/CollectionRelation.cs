using System;
using System.ComponentModel.DataAnnotations;

namespace Milky.OsuPlayer.Data.Models
{
    public class CollectionRelation : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        // fk
        public Collection Collection { get; set; }
        public Guid CollectionId { get; set; }
        public Beatmap Beatmap { get; set; }
        public byte[] BeatmapId { get; set; }
    }
}