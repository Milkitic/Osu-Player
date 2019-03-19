using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.WpfApi;
using System.Collections.Generic;

namespace Milky.OsuPlayer.Common.Player
{
    public class CurrentInfo : ViewModelBase
    {
        private bool _isFavourite;
        public CurrentInfo() { }

        public CurrentInfo(string artist, string artistUnicode, string title, string titleUnicode, string creator,
            string source, List<string> tags, int beatmapId, int beatmapsetId, double stars, double hp, double cs, double ar,
            double od, long songLength, MapIdentity identity, MapInfo mapInfo, Beatmap beatmap, bool isFavourite)
        {
            Artist = artist;
            ArtistUnicode = artistUnicode;
            Title = title;
            TitleUnicode = titleUnicode;
            Creator = creator;
            Source = source;
            Tags = tags;
            BeatmapId = beatmapId;
            BeatmapsetId = beatmapsetId;
            Stars = stars;
            HP = hp;
            CS = cs;
            AR = ar;
            OD = od;
            SongLength = songLength;
            Identity = identity;
            MapInfo = mapInfo;
            Beatmap = beatmap;
            IsFavourite = isFavourite;
        }

        public string Artist { get; set; }
        public string ArtistUnicode { get; set; }
        public string Title { get; set; }
        public string TitleUnicode { get; set; }
        public string Creator { get; set; }
        public string Source { get; set; }
        public List<string> Tags { get; set; }
        public int BeatmapId { get; set; }
        public int BeatmapsetId { get; set; }
        public double Stars { get; set; }
        // ReSharper disable once InconsistentNaming
        public double HP { get; set; }
        // ReSharper disable once InconsistentNaming
        public double CS { get; set; }
        // ReSharper disable once InconsistentNaming
        public double AR { get; set; }
        // ReSharper disable once InconsistentNaming
        public double OD { get; set; }
        public long SongLength { get; set; }
        public MapIdentity Identity { get; set; }
        public MapInfo MapInfo { get; }
        public Beatmap Beatmap { get; }

        public bool IsFavourite
        {
            get => _isFavourite;
            set
            {
                _isFavourite = value;
                OnPropertyChanged();
            }
        }
    }
}
