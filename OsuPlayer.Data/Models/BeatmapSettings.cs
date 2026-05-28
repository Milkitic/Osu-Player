using System;
using Coosu.Beatmap.MetaData;
using Dapper.FluentMap.Mapping;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapSettingsMap : EntityMap<BeatmapSettings>
    {
        public BeatmapSettingsMap()
        {
            Map(p => p.Id).ToColumn("id");
            Map(p => p.Version).ToColumn("difficulty_name");
            Map(p => p.FolderName).ToColumn("folder_name");
            Map(p => p.InOwnDb).ToColumn("is_local");
            Map(p => p.Offset).ToColumn("audio_offset_ms");
            Map(p => p.LastPlayTime).ToColumn("last_played_at");
            Map(p => p.ExportFile).ToColumn("exported_file_path");
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
