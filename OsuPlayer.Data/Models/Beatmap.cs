using System;
using System.Collections.Generic;
using Coosu.Beatmap;
using Coosu.Beatmap.MetaData;
using Dapper.FluentMap.Mapping;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapMap : EntityMap<Beatmap>
    {
        public BeatmapMap()
        {
            Map(p => p.Artist).ToColumn("artist");
            Map(p => p.Title).ToColumn("title");
            Map(p => p.ArtistUnicode).ToColumn("artistU");
            Map(p => p.TitleUnicode).ToColumn("titleU");
            Map(p => p.Creator).ToColumn("creator");
            Map(p => p.BeatmapFileName).ToColumn("fileName");
            Map(p => p.LastModifiedTime).ToColumn("lastModified");
            Map(p => p.DiffSrNoneStandard).ToColumn("diffSrStd");
            Map(p => p.DiffSrNoneTaiko).ToColumn("diffSrTaiko");
            Map(p => p.DiffSrNoneCtB).ToColumn("diffSrCtb");
            Map(p => p.DiffSrNoneMania).ToColumn("diffSrMania");
            Map(p => p.DrainTimeSeconds).ToColumn("drainTime");
            Map(p => p.TotalTime).ToColumn("totalTime");
            Map(p => p.AudioPreviewTime).ToColumn("audioPreview");
            Map(p => p.BeatmapId).ToColumn("beatmapId");
            Map(p => p.BeatmapSetId).ToColumn("beatmapSetId");
            Map(p => p.GameMode).ToColumn("gameMode");
            Map(p => p.SongSource).ToColumn("source");
            Map(p => p.SongTags).ToColumn("tags");
            Map(p => p.FolderName).ToColumn("folderName");
            Map(p => p.AudioFileName).ToColumn("audioName");
            Map(p => p.Id).ToColumn("id");
            Map(p => p.InOwnDb).ToColumn("own");
            Map(p => p.Version).ToColumn("version");
        }
    }

    public class Beatmap : IMapIdentifiable, IEquatable<Beatmap>
    {
        public string Artist { get; set; }
        public string ArtistUnicode { get; set; }
        public string Title { get; set; }
        public string TitleUnicode { get; set; }
        public string Creator { get; set; } //mapper
        public string Version { get; set; } //difficulty name
        public string AudioFileName { get; set; }
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
        public Coosu.Beatmap.Sections.GamePlay.GameMode GameMode { get; set; }
        public string SongSource { get; set; }
        public string SongTags { get; set; }
        public string FolderName { get; set; } = "";
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool InOwnDb { get; set; }

        public string AutoTitle => MetaString.GetUnicode(Title, TitleUnicode) ?? "未知标题";
        public string AutoArtist => MetaString.GetUnicode(Artist, ArtistUnicode) ?? "未知艺术家";

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

        public override int GetHashCode()
        {
            return (FolderName + Version).GetHashCode();
        }

        public MapIdentity GetIdentity()
        {
            return new MapIdentity(FolderName, Version, InOwnDb);
        }

        public class Comparer : IEqualityComparer<Beatmap>
        {
            private readonly bool _byIdentity;

            public Comparer(bool byIdentity)
            {
                _byIdentity = byIdentity;
            }

            public bool Equals(Beatmap x, Beatmap y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;

                if (_byIdentity)
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

        public bool Equals(Beatmap other)
        {
            return FolderName == other?.FolderName && Version == other?.Version && InOwnDb == other?.InOwnDb;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Beatmap)obj);
        }
    }

    public class JoinedBeatmap : Beatmap
    {
        public string FileSize { get; set; }
        public string ExportTime { get; set; }
        public string ExportFile { get; set; }
    }
}
