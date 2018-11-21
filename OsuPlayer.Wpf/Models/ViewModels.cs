using Milkitic.OsuPlayer.Utils;
using osu.Shared;
using System;

namespace Milkitic.OsuPlayer
{
    public class BeatmapDataModel
    {
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
        public string BeatmapFileName { get; set; }
        public double Stars { get; set; }

        //Extended
        public string Id { get; set; }
        public string AutoTitleSource =>
            (MetaSelect.GetUnicode(Title, TitleUnicode) +
             (string.IsNullOrEmpty(SongSource) ? "" : $"\r\n\r\n —— {SongSource}")).Replace("_", "__");
        public string AutoTitle => MetaSelect.GetUnicode(Title, TitleUnicode).Replace("_", "__");
        public string AutoArtist => MetaSelect.GetUnicode(Artist, ArtistUnicode).Replace("_", "__");
        public string AutoCreator => Creator.Replace("_", "__");
        public string AutoVersion => Version.Replace("_", "__");
        public string FileSize { get; set; }
        public string ExportTime { get; set; }
        public string ExportFile { get; set; }
    }
}
