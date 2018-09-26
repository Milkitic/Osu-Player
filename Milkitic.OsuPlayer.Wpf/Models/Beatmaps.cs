using Milkitic.OsuPlayer.Wpf.Utils;
using osu.Shared;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Wpf.Models
{
    public struct BeatmapSearchInfo
    {
        public int Id { get; set; }

        public string Artist { get; set; }
        public string ArtistUnicode { get; set; }
        public string Title { get; set; }
        public string TitleUnicode { get; set; }

        public string Creator { get; set; } //mapper
        public string SongSource { get; set; }
        public string SongTags { get; set; }
        public string Version { get; set; } //difficulty name
        public GameMode GameMode { get; set; }
        public int BeatmapId { get; set; }
        public string FolderName { get; set; }
        public string AutoTitleSource =>
            (MetaSelect.GetUnicode(Title, TitleUnicode) +
             (string.IsNullOrEmpty(SongSource) ? "" : $"\r\n\r\n —— {SongSource}")).Replace("_", "__");
        public string AutoTitle => MetaSelect.GetUnicode(Title, TitleUnicode).Replace("_", "__");
        public string AutoArtist => MetaSelect.GetUnicode(Artist, ArtistUnicode).Replace("_", "__");
        public string AutoCreator => Creator.Replace("_", "__");
        public string AutoVersion => Version.Replace("_", "__");
        public void SetId(int value) => Id = value;
    }
}
