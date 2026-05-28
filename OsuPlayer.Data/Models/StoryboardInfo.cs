using Coosu.Beatmap.MetaData;
using Dapper.FluentMap.Mapping;

namespace Milky.OsuPlayer.Data.Models
{
    public class StoryboardInfoMap : EntityMap<StoryboardInfo>
    {
        public StoryboardInfoMap()
        {
            Map(p => p.Id).ToColumn("id");
            Map(p => p.MapId).ToColumn("beatmap_id");
            Map(p => p.SbThumbPath).ToColumn("thumbnail_path");
            Map(p => p.SbThumbVideoPath).ToColumn("preview_video_path");
            Map(p => p.Version).ToColumn("difficulty_name");
            Map(p => p.FolderName).ToColumn("folder_name");
            Map(p => p.InOwnDb).ToColumn("is_local");
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
