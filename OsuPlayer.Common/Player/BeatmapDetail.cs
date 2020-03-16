using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.WpfApi;
using System.Collections.Generic;
using Milky.OsuPlayer.Common.Data;
using OSharp.Beatmap;

namespace Milky.OsuPlayer.Common.Player
{
    public class BeatmapDetail : ViewModelBase
    {
        public class MetaDetail : ViewModelBase
        {
            private List<string> _tags;
            public MetaString Artist { get; set; }
            public MetaString Title { get; set; }
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
