using System.Collections.Generic;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data.EF.Model;
using osu_database_reader.Components.Beatmaps;

namespace Milky.OsuPlayer.Models
{
    public class CurrentInfo
    {
        public CurrentInfo() { }

        public CurrentInfo(string artist, string artistUnicode, string title, string titleUnicode, string creator,
            string source, List<string> tags, int beatmapId, int beatmapsetId, double stars, double hp, double cs, double ar,
            double od, long songLength, MapIdentity identity, MapInfo mapInfo, BeatmapEntry entry, bool isFaved)
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
            Entry = entry;
            IsFaved = isFaved;
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
        public BeatmapEntry Entry { get; }
        public bool IsFaved { get; set; }
    }
}
