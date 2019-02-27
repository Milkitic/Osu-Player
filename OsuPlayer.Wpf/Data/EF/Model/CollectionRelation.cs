using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milky.OsuPlayer.Data.EF.Model
{
    [Table("collection_relation")]
    public class CollectionRelation
    {
        public CollectionRelation() { }

        public CollectionRelation(string id, string collectionId, string mapId)
        {
            Id = id;
            CollectionId = collectionId;
            MapId = mapId;
            AddTime = DateTime.Now;
        }

        [Required, Column("id")]
        public string Id { get; set; }
        [Required, Column("collectionId")]
        public string CollectionId { get; set; }
        [Required, Column("mapId")]
        public string MapId { get; set; }
        [Column("addTime")]
        public DateTime? AddTime { get; set; }
    }
}