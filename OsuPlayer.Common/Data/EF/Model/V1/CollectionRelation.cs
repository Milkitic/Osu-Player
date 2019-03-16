using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Milky.OsuPlayer.Common.Data.EF.Model.V1
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
        [JsonProperty("id")]
        public string Id { get; set; }

        [Required, Column("collectionId")]
        [JsonProperty("collectionId")]
        public string CollectionId { get; set; }

        [Required, Column("mapId")]
        [JsonProperty("mapId")]
        public string MapId { get; set; }

        [Column("addTime")]
        [JsonProperty("addTime")]
        public DateTime? AddTime { get; set; }
    }
}