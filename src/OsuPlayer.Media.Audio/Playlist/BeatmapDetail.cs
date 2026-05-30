using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Coosu.Beatmap;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.Media.Audio.Playlist;

public partial class BeatmapDetail : ObservableObject
{
    public partial class MetaDetail : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ArtistAuto))]
        [NotifyPropertyChangedFor(nameof(ArtistAscii))]
        [NotifyPropertyChangedFor(nameof(ArtistUnicode))]
        public partial MetaString Artist { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TitleAuto))]
        [NotifyPropertyChangedFor(nameof(TitleAscii))]
        [NotifyPropertyChangedFor(nameof(TitleUnicode))]
        public partial MetaString Title { get; set; }

        public string Creator { get; set; }
        public string Version { get; set; }
        public string Source { get; set; }

        [ObservableProperty]
        public partial List<string> Tags { get; set; }

        partial void OnTagsChanged(List<string> value)
        {
            TagString = value == null ? string.Empty : string.Join(" ", value);
        }

        public string TagString { get; set; }
        public int BeatmapId { get; set; }
        public int BeatmapsetId { get; set; }

        public bool IsFavorite { get; set; }

        // ReSharper disable InconsistentNaming
        public double HP { get; set; }
        public double CS { get; set; }
        public double AR { get; set; }
        public double OD { get; set; }
        // ReSharper restore InconsistentNaming

        public string ArtistAuto => Artist.ToPreferredString();
        public string TitleAuto => Title.ToPreferredString();
        public string ArtistAscii => Artist.ToOriginalString();
        public string ArtistUnicode => Artist.ToUnicodeString();
        public string TitleAscii => Title.ToOriginalString();
        public string TitleUnicode => Title.ToUnicodeString();
    }

    public MetaDetail Metadata { get; } = new MetaDetail();

    public BeatmapDetail(Beatmap beatmap)
    {
        Beatmap = beatmap;
    }

    public Beatmap Beatmap { get; }
    public double Stars { get; set; }
    public long SongLength { get; set; }
    public MapIdentity Identity => Beatmap.GetIdentity();

    public string BaseFolder { get; set; }
    public string MapPath { get; set; }
    public string BackgroundPath { get; set; }
    public string MusicPath { get; set; }
    public string VideoPath { get; set; }
    public string StoryboardPath { get; set; }
}