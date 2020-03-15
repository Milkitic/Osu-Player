using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.WpfApi;
using System.Collections.Generic;
using OSharp.Beatmap;

namespace Milky.OsuPlayer.Common.Player
{
    public class BeatmapDetail : ViewModelBase
    {
        private bool _isFavorite;
        private string _artist;
        private string _artistUnicode;
        private string _title;
        private string _titleUnicode;
        public BeatmapDetail() { }

        public BeatmapDetail(string artist, string artistUnicode, string title, string titleUnicode, string creator,
            string source, List<string> tags, int beatmapId, int beatmapsetId, double stars, double hp, double cs,
            double ar,
            double od, long songLength, MapIdentity identity, MapInfo mapInfo, Beatmap beatmap, bool isFavorite,
            string path, string bgPath)
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
            IsFavorite = isFavorite;
            Path = path;
            BgPath = bgPath;
            TagString = string.Join(" ", Tags);
        }

        public string Artist
        {
            get => _artist;
            set
            {
                _artist = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ArtistAuto));
            }
        }

        public string ArtistUnicode
        {
            get => _artistUnicode;
            set
            {
                _artistUnicode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ArtistAuto));
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TitleAuto));
            }
        }

        public string TitleUnicode
        {
            get => _titleUnicode;
            set
            {
                _titleUnicode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TitleAuto));
            }
        }

        public string ArtistAuto => MetaString.GetUnicode(Artist, ArtistUnicode);
        public string TitleAuto => MetaString.GetUnicode(Title, TitleUnicode);

        public string Creator { get; set; }
        public string Source { get; set; }
        public List<string> Tags { get; set; }
        public string TagString { get; set; }
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

        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                _isFavorite = value;
                OnPropertyChanged();
            }
        }

        public string Path { get; set; }
        public string BgPath { get; }
    }
}
