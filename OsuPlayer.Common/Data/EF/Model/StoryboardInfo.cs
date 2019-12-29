using System;
using Dapper.FluentMap.Mapping;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.Common.Data.EF.Model
{
    public class StoryboardInfoMap : EntityMap<StoryboardInfo>
    {
        public StoryboardInfoMap()
        {
            Map(p => p.Id).ToColumn("id");
            Map(p => p.BeatmapDbId).ToColumn("mapId");
            Map(p => p.Version).ToColumn("version");
            Map(p => p.FolderName).ToColumn("folder");
        }
    }

    public class StoryboardFullInfoMap : EntityMap<StoryboardFullInfo>
    {
        public StoryboardFullInfoMap()
        {
            Map(p => p.Id).ToColumn("id");
            Map(p => p.SbThumbPath).ToColumn("thumbPath");
            Map(p => p.SbThumbVideoPath).ToColumn("thumbVideoPath");
            Map(p => p.FolderName).ToColumn("folder");
            Map(p => p.InOwnFolder).ToColumn("own");
        }
    }

    public class StoryboardInfo : IMapIdentifiable
    {
        public StoryboardInfo()
        {

        }

        public StoryboardInfo(string version, string folderName)
        {
            Version = version;
            FolderName = folderName;
        }

        public string Id { get; set; }
        public string BeatmapDbId { get; set; }
        public string Version { get; set; }
        public string FolderName { get; set; }
    }

    public class StoryboardFullInfo
    {
        public StoryboardFullInfo()
        {

        }

        public StoryboardFullInfo(string folderName, bool inOwnFolder)
        {
            FolderName = folderName;
            InOwnFolder = inOwnFolder;
        }

        public string Id { get; set; }
        public string SbThumbPath { get; set; }
        public string SbThumbVideoPath { get; set; }
        public string FolderName { get; set; }
        public bool InOwnFolder { get; set; }
    }
}