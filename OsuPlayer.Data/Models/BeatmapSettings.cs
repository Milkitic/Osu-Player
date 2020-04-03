using Dapper.FluentMap.Mapping;
using OSharp.Beatmap.MetaData;
using System;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapSettingsMap : EntityMap<BeatmapSettings>
    {
        public BeatmapSettingsMap()
        {
            Map(p => p.Id).ToColumn("id");
            Map(p => p.Version).ToColumn("version");
            Map(p => p.FolderName).ToColumn("folder");
            Map(p => p.InOwnDb).ToColumn("ownDb");
            Map(p => p.Offset).ToColumn("offset");
            Map(p => p.LastPlayTime).ToColumn("lastPlayTime");
            Map(p => p.ExportFile).ToColumn("exportFile");
            Map(p => p.AddTime).ToColumn("addTime");
        }
    }

    public class BeatmapSettings : IMapIdentifiable
    {
        public BeatmapSettings() { }

        public BeatmapSettings(string id, string version, string folderName, int offset, DateTime? lastPlayTime,
            string exportFile = null, DateTime? addTime = null)
        {
            Id = id;
            Version = version;
            FolderName = folderName;
            Offset = offset;
            LastPlayTime = lastPlayTime;
            AddTime = addTime;
            if (exportFile != null)
                ExportFile = exportFile;
        }

        public string Id { get; set; }

        public string Version { get; set; }

        public string FolderName { get; set; }

        public bool InOwnDb { get; set; }

        public int Offset { get; set; }

        public DateTime? LastPlayTime { get; set; }

        public string ExportFile { get; set; }

        //Extension
        public DateTime? AddTime { get; }

        public MapIdentity GetIdentity()
        {
            return new MapIdentity(FolderName, Version, InOwnDb);
        }
    }
}