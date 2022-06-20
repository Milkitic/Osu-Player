using Coosu.Beatmap.MetaData;
using Dapper.FluentMap.Mapping;

namespace Milky.OsuPlayer.Data.Models
{
    public class StoryboardInfoMap : EntityMap<StoryboardInfo>
    {
        public StoryboardInfoMap()
        {
            Map(p => p.Id).ToColumn("id");
            Map(p => p.MapId).ToColumn("mapId");
            Map(p => p.SbThumbPath).ToColumn("thumbPath");
            Map(p => p.SbThumbVideoPath).ToColumn("thumbVideoPath");
            Map(p => p.Version).ToColumn("version");
            Map(p => p.FolderName).ToColumn("folder");
            Map(p => p.InOwnDb).ToColumn("ownDb");
        }
    }

    public class StoryboardInfo : IMapIdentifiable
    {
        public string Id { get; set; }
        public string MapId { get; set; }
        public string SbThumbPath { get; set; }
        public string SbThumbVideoPath { get; set; }
        public string Version { get; set; }
        public string FolderName { get; set; }
        public bool InOwnDb { get; set; }

        public MapIdentity GetIdentity()
        {
            return new MapIdentity(FolderName, Version, InOwnDb);
        }
    }
}