using OSharp.Beatmap;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace Milky.OsuPlayer.Data.Models
{
    public class Beatmap : BaseEntity, IEquatable<Beatmap>
    {
        private BeatmapThumb _beatmapThumb;

        public class Comparer : IEqualityComparer<Beatmap>
        {
            private readonly bool _isByIdentity;

            public Comparer(bool isByIdentity)
            {
                _isByIdentity = isByIdentity;
            }

            public bool Equals(Beatmap x, Beatmap y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;

                if (_isByIdentity)
                {
                    return x.Equals(y);
                }

                return x.Id == y.Id; //todo: sb
            }

            public int GetHashCode(Beatmap obj)
            {
                return obj.GetHashCode();
            }
        }

        // string.Concat(Folder, Version, InOwnDb)
        [Key]
        public string Id { get; set; }
        public string Artist { get; set; }
        public string ArtistUnicode { get; set; }
        public string Title { get; set; }
        public string TitleUnicode { get; set; }

        public string Creator { get; set; } //mapper
        public string Version { get; set; } //difficulty name
        public string BeatmapFileName { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public double DiffSrNoneStandard { get; set; }
        public double DiffSrNoneTaiko { get; set; }
        public double DiffSrNoneCtB { get; set; }
        public double DiffSrNoneMania { get; set; }
        public int DrainTimeSeconds { get; set; } //NOTE: in s
        public int TotalTime { get; set; } //NOTE: in ms
        public int AudioPreviewTime { get; set; } //NOTE: in ms
        public int BeatmapId { get; set; }
        public int BeatmapSetId { get; set; }
        public OSharp.Beatmap.Sections.GamePlay.GameMode GameMode { get; set; }
        public string SongSource { get; set; }
        public string SongTags { get; set; }
        public string FolderNameOrPath { get; set; }
        public string AudioFileName { get; set; }
        public bool InOwnDb { get; set; }

        [NotMapped]
        public string PreferredArtist => MetaString.GetUnicode(Artist, ArtistUnicode);
        [NotMapped]
        public string PreferredTitle => MetaString.GetUnicode(Title, TitleUnicode);

        //public Guid? BeatmapConfigId { get; set; }
        public BeatmapConfig BeatmapConfig { get; set; }

        //public Guid? BeatmapExportId { get; set; }
        public BeatmapExport BeatmapExport { get; set; }

        //public Guid? BeatmapStoryboardId { get; set; }
        public BeatmapStoryboard BeatmapStoryboard { get; set; }

        //public Guid? BeatmapThumbId { get; set; }

        public BeatmapThumb BeatmapThumb
        {
            get => _beatmapThumb;
            set
            {
                if (Equals(value, _beatmapThumb)) return;
                _beatmapThumb = value;
                OnPropertyChanged();
            }
        }

        public List<Collection> Collections { get; set; }

        [NotMapped] public bool IsTemporary => Id?.StartsWith('!') != true;

        public override string ToString()
        {
            if (this.IsTemporary)
                return $"temp: \"{FolderNameOrPath}\"";

            if (InOwnDb)
                return $"own: [\"{FolderNameOrPath}\",\"{Version}\"]";

            return $"osu: [\"{FolderNameOrPath}\",\"{Version}\"]";
        }

        public bool Equals(Beatmap other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return FolderNameOrPath == other.FolderNameOrPath && Version == other.Version && InOwnDb == other.InOwnDb;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Beatmap)obj);
        }

        public override int GetHashCode()
        {
            var sign = Path.IsPathRooted(FolderNameOrPath) ? -1 : 1;
            return sign * HashCode.Combine(FolderNameOrPath, Version, InOwnDb);
        }

        public static bool operator ==(Beatmap left, Beatmap right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Beatmap left, Beatmap right)
        {
            return !Equals(left, right);
        }

        #region Only used in HoLLy

        //public string BeatmapChecksum { get; set; }
        //public RankStatus RankedStatus { get; set; }
        //public ushort CountHitCircles { get; set; }
        //public ushort CountSliders { get; set; }
        //public ushort CountSpinners { get; set; }
        //public float ApproachRate { get; set; }
        //public float CircleSize { get; set; }
        //public float HpDrainRate { get; set; }
        //public float OveralDifficulty { get; set; }
        //public double SliderVelocity { get; set; }
        //public List<Timing> TimingPoints { get; set; }
        //public int ThreadId { get; set; } //what's this?
        //public Rank GradeStandard { get; set; }
        //public Rank GradeTaiko { get; set; }
        //public Rank GradeCtB { get; set; }
        //public Rank GradeMania { get; set; }
        //public short OffsetLocal { get; set; }
        //public float StackLeniency { get; set; }
        //public short OffsetOnline { get; set; }
        //public string TitleFont { get; set; }
        //public bool Unplayed { get; set; }
        //public DateTime LastPlayed { get; set; }
        //public bool IsOsz2 { get; set; }
        //public DateTime LastCheckAgainstOsuRepo { get; set; }
        //public bool IgnoreBeatmapSounds { get; set; }
        //public bool IgnoreBeatmapSkin { get; set; }
        //public bool DisableStoryBoard { get; set; }
        //public bool DisableVideo { get; set; }
        //public bool VisualOverride { get; set; }
        //public byte ManiaScrollSpeed { get; set; }

        #endregion
    }
}
