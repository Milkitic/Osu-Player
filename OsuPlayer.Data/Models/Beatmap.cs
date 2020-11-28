using OSharp.Beatmap;
using OSharp.Beatmap.MetaData;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace Milky.OsuPlayer.Data.Models
{
    public class Beatmap : BaseEntity, IMapIdentifiable, IEquatable<Beatmap>
    {
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

        public Guid Id { get; set; } = Guid.NewGuid();
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

        public List<Collection> Collections { get; set; }

        string IMapIdentifiable.FolderName => FolderNameOrPath;

        [NotMapped]
        public bool IsTemporary { get; set; }

        [NotMapped]
        public bool IsMapTemporary => Path.IsPathRooted(FolderNameOrPath);

        public MapIdentity GetIdentity()
        {
            return new MapIdentity(FolderNameOrPath, Version, InOwnDb);
        }

        public bool Equals(Beatmap other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Creator == other.Creator && Version == other.Version && InOwnDb == other.InOwnDb;
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
            return HashCode.Combine(Creator, Version, InOwnDb);
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
