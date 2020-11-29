using System;

namespace Milky.OsuPlayer.Data.Models
{
    public class CollectionRelation : BaseEntity
    {
        public Guid Id { get; set; }

        // fk
        public Collection Collection { get; set; }
        public Guid CollectionId { get; set; }
        public Beatmap Beatmap { get; set; }
        public int BeatmapId { get; set; }
    }
}