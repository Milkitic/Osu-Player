using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.Data.EF.Model
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
            if (exportFile != null) ExportFile = exportFile;
        }

        [Required, Column("id")]
        public string Id { get; set; }
        [Required, Column("version")]
        public string Version { get; set; }
        [Required, Column("folder")]
        public string FolderName { get; set; }
        [Column("offset")]
        public int Offset { get; set; }
        [Column("lastPlayTime")]
        public DateTime? LastPlayTime { get; set; }
        [Column("exportFile")]
        public string ExportFile { get; set; }

        //Extension
        public DateTime? AddTime { get; }

    }
}