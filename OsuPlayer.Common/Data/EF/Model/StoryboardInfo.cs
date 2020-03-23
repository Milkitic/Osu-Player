using Dapper.FluentMap.Mapping;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.Common.Data.EF.Model
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
    }
}