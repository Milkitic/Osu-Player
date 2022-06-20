using System.Collections.Generic;
using Coosu.Beatmap;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation.Interaction;

namespace Milky.OsuPlayer.Media.Audio.Playlist
{
    public class BeatmapDetail : VmBase
    {
        public class MetaDetail : VmBase
        {
            private List<string> _tags;
            private MetaString _artist;
            private MetaString _title;

            public MetaString Artist
            {
                get => _artist;
                set
                {
                    if (value.Equals(_artist)) return;
                    _artist = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ArtistAuto));
                }
            }

            public MetaString Title
            {
                get => _title;
                set
                {
                    if (value.Equals(_title)) return;
                    _title = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TitleAuto));
                }
            }

            public string Creator { get; set; }
            public string Version { get; set; }
            public string Source { get; set; }

            public List<string> Tags
            {
                get => _tags;
                set
                {
                    if (Equals(value, _tags)) return;
                    _tags = value;
                    TagString = string.Join(" ", value);
                    OnPropertyChanged();
                }
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
}
