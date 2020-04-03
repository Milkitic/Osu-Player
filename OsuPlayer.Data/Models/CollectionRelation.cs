using System;
using Dapper.FluentMap.Mapping;

namespace Milky.OsuPlayer.Data.Models
{
    public class CollectionRelationMap : EntityMap<CollectionRelation>
    {
        public CollectionRelationMap()
        {
            Map(p => p.Id).ToColumn("id");
            Map(p => p.CollectionId).ToColumn("collectionId");
            Map(p => p.MapId).ToColumn("mapId");
            Map(p => p.AddTime).ToColumn("addTime");
        }
    }

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

        public string Id { get; set; }
        public string CollectionId { get; set; }
        public string MapId { get; set; }
        public DateTime? AddTime { get; set; }
    }
}