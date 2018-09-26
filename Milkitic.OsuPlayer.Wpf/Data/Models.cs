using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Wpf.Data
{
    [Table("map_info")]
    public class MapInfo
    {
        public MapInfo() { }

        public MapInfo(string id, string version, string folder, int offset,DateTime? lastPlayTime)
        {
            Id = id;
            Version = version;
            Folder = folder;
            Offset = offset;
            LastPlayTime = lastPlayTime;
        }

        [Required, Column("id")]
        public string Id { get; set; }
        [Required, Column("version")]
        public string Version { get; set; }
        [Required, Column("folder")]
        public string Folder { get; set; }
        [Column("offset")]
        public int Offset { get; set; }
        [Column("lastPlayTime")]
        public DateTime? LastPlayTime { get; set; }
    }

    public class MapInfoContext : DbContext
    {
        public DbSet<MapInfo> MapInfos { get; set; }
        public MapInfoContext() : base("sqlite") { }
    }

    [Table("collection")]
    public class Collection
    {
        public Collection() { }

        public Collection(string id, string name)
        {
            Id = id;
            Name = name;
        }

        [Required, Column("id")]
        public string Id { get; set; }
        [Required, Column("name")]
        public string Name { get; set; }
    }

    public class CollectionContext : DbContext
    {
        public DbSet<Collection> Collections { get; set; }
        public CollectionContext() : base("sqlite") { }
    }

    [Table("collection_relation")]
    public class CollectionRelation
    {
        public CollectionRelation() { }

        public CollectionRelation(string id, string collectionId, string mapId)
        {
            Id = id;
            CollectionId = collectionId;
            MapId = mapId;
        }

        [Required, Column("id")]
        public string Id { get; set; }
        [Required, Column("collectionId")]
        public string CollectionId { get; set; }
        [Required, Column("mapId")]
        public string MapId { get; set; }

    }

    public class RelationContext : DbContext
    {
        public DbSet<CollectionRelation> Relations { get; set; }
        public RelationContext() : base("sqlite") { }
    }
}