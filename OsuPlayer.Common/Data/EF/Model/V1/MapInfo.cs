using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.Common.Data.EF.Model.V1
{
    [Table("map_info")]
    public class MapInfo : IMapIdentifiable
    {
        public MapInfo() { }

        public MapInfo(string id, string version, string folderName, int offset, DateTime? lastPlayTime,
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

        [Required, Column("id")]
        [JsonProperty("id")]
        public string Id { get; set; }

        [Required, Column("version")]
        [JsonProperty("version")]
        public string Version { get; set; }

        [Required, Column("folder")]
        [JsonProperty("folder")]
        public string FolderName { get; set; }

        [Column("offset")]
        [JsonProperty("offset")]
        public int Offset { get; set; }

        [Column("lastPlayTime")]
        [JsonProperty("lastPlayTime")]
        public DateTime? LastPlayTime { get; set; }

        //[Column("playCount")]
        //[JsonProperty("playCount")]
        //public int PlayCount { get; set; } = 0;

        [Column("exportFile")]
        [JsonProperty("exportFile")]
        public string ExportFile { get; set; }

        //Extension
        [JsonProperty("addTime")]
        //[Column("addTime")]
        public DateTime? AddTime { get; }

    }
}