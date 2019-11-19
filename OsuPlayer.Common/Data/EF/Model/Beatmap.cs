using OSharp.Beatmap;
using OSharp.Beatmap.MetaData;
using osu.Shared;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Dapper.FluentMap.Mapping;

namespace Milky.OsuPlayer.Common.Data.EF.Model
{
    public class BeatmapMap : EntityMap<Beatmap>
    {
        public BeatmapMap()
        {
            Map(p => p.ArtistUnicode).ToColumn("artistU");
            Map(p => p.TitleUnicode).ToColumn("titleU");
            Map(p => p.BeatmapFileName).ToColumn("fileName");
            Map(p => p.LastModifiedTime).ToColumn("lastModified");
            Map(p => p.DiffSrNoneStandard).ToColumn("diffSrStd");
            Map(p => p.DiffSrNoneTaiko).ToColumn("diffSrTaiko");
            Map(p => p.DiffSrNoneCtB).ToColumn("diffSrCtb");
            Map(p => p.DiffSrNoneMania).ToColumn("diffSrMania");
            Map(p => p.DrainTimeSeconds).ToColumn("drainTime");
            Map(p => p.AudioPreviewTime).ToColumn("audioPreview");
            Map(p => p.SongSource).ToColumn("source");
            Map(p => p.SongTags).ToColumn("tags");
            Map(p => p.AudioFileName).ToColumn("audioName");
            Map(p => p.InOwnFolder).ToColumn("own");
            Map(p => p.Version).ToColumn("version");
        }
    }

    public class Beatmap : IMapIdentifiable
    {
        [Column("artist")]
        public string Artist { get; set; }

        [Column("artistU")]
        public string ArtistUnicode { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("titleU")]
        public string TitleUnicode { get; set; }

        [Column("creator")]
        public string Creator { get; set; } //mapper

        [Column("version")]
        public string Version { get; set; } //difficulty name

        [Column("fileName")]
        public string BeatmapFileName { get; set; }

        [Column("lastModified")]
        public DateTime LastModifiedTime { get; set; }

        [Column("diffSrStd")]
        public double DiffSrNoneStandard { get; set; }

        [Column("diffSrTaiko")]
        public double DiffSrNoneTaiko { get; set; }

        [Column("diffSrCtb")]
        public double DiffSrNoneCtB { get; set; }

        [Column("diffSrMania")]
        public double DiffSrNoneMania { get; set; }

        [Column("drainTime")]
        public int DrainTimeSeconds { get; set; } //NOTE: in s

        [Column("totalTime")]
        public int TotalTime { get; set; } //NOTE: in ms

        [Column("audioPreview")]
        public int AudioPreviewTime { get; set; } //NOTE: in ms

        [Column("beatmapId")]
        public int BeatmapId { get; set; }

        [Column("beatmapSetId")]
        public int BeatmapSetId { get; set; }

        [Column("gameMode")]
        public OSharp.Beatmap.Sections.GamePlay.GameMode GameMode { get; set; }

        [Column("source")]
        public string SongSource { get; set; }

        [Column("tags")]
        public string SongTags { get; set; }

        [Column("folderName")]
        public string FolderName { get; set; } = "";

        [Column("audioName")]
        public string AudioFileName { get; set; }

        [Key]
        [Required, Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, Column("own")]
        public bool InOwnFolder { get; set; }

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

        public Beatmap UpdateFromHolly(BeatmapEntry entry)
        {
            Artist = entry.Artist;
            ArtistUnicode = entry.ArtistUnicode;
            Title = entry.Title;
            TitleUnicode = entry.TitleUnicode;
            Creator = entry.Creator;
            Version = entry.Version;
            BeatmapFileName = entry.BeatmapFileName;
            LastModifiedTime = entry.LastModifiedTime;
            DiffSrNoneStandard = entry.DiffStarRatingStandard.ContainsKey(Mods.None)
                ? entry.DiffStarRatingStandard[Mods.None]
                : -1;
            DiffSrNoneTaiko = entry.DiffStarRatingTaiko.ContainsKey(Mods.None)
                ? entry.DiffStarRatingTaiko[Mods.None]
                : -1;
            DiffSrNoneCtB = entry.DiffStarRatingCtB.ContainsKey(Mods.None) ? entry.DiffStarRatingCtB[Mods.None] : -1;
            DiffSrNoneMania = entry.DiffStarRatingMania.ContainsKey(Mods.None)
                ? entry.DiffStarRatingMania[Mods.None]
                : -1;
            DrainTimeSeconds = entry.DrainTimeSeconds;
            TotalTime = entry.TotalTime;
            AudioPreviewTime = entry.AudioPreviewTime;
            BeatmapId = entry.BeatmapId;
            BeatmapSetId = entry.BeatmapSetId;
            GameMode = entry.GameMode.ParseHollyToOSharp();
            SongSource = entry.SongSource;
            SongTags = entry.SongTags;
            FolderName = entry.FolderName;
            AudioFileName = entry.AudioFileName;

            return this;
        }

        public override int GetHashCode()
        {
            return (FolderName + Version).GetHashCode();
        }

        public static Beatmap ParseFromHolly(BeatmapEntry entry)
        {
            return (new Beatmap()).UpdateFromHolly(entry);
        }

        public Beatmap UpdateFromOSharp(OsuFile osuFile)
        {
            Artist = osuFile.Metadata.Artist;
            ArtistUnicode = osuFile.Metadata.ArtistUnicode;
            Title = osuFile.Metadata.Title;
            TitleUnicode = osuFile.Metadata.TitleUnicode;
            Creator = osuFile.Metadata.Creator;
            Version = osuFile.Metadata.Version;
            //BeatmapFileName = osuFile.BeatmapFileName;
            //LastModifiedTime = osuFile.LastModifiedTime;
            //DiffSrNoneStandard = osuFile.DiffStarRatingStandard.ContainsKey(Mods.None)
            //    ? osuFile.DiffStarRatingStandard[Mods.None]
            //    : -1;
            //DiffSrNoneTaiko = osuFile.DiffStarRatingTaiko.ContainsKey(Mods.None)
            //    ? osuFile.DiffStarRatingTaiko[Mods.None]
            //    : -1;
            //DiffSrNoneCtB = osuFile.DiffStarRatingCtB.ContainsKey(Mods.None) ? osuFile.DiffStarRatingCtB[Mods.None] : -1;
            //DiffSrNoneMania = osuFile.DiffStarRatingMania.ContainsKey(Mods.None)
            //    ? osuFile.DiffStarRatingMania[Mods.None]
            //    : -1;
            DrainTimeSeconds = (int)(osuFile.HitObjects.MaxTime -
                                     osuFile.HitObjects.MinTime -
                                     osuFile.Events.Breaks.Select(k => k.EndTime - k.StartTime).Sum());
            TotalTime = (int)osuFile.HitObjects.MaxTime;
            AudioPreviewTime = osuFile.General.PreviewTime;
            BeatmapId = osuFile.Metadata.BeatmapId;
            BeatmapSetId = osuFile.Metadata.BeatmapSetId;
            GameMode = osuFile.General.Mode;
            SongSource = osuFile.Metadata.Source;
            SongTags = string.Join(" ", osuFile.Metadata.TagList);
            //FolderName = osuFile.FolderName;
            AudioFileName = osuFile.General.AudioFilename;

            return this;
        }

        public static Beatmap ParseFromOSharp(OsuFile osuFile)
        {
            return (new Beatmap()).UpdateFromOSharp(osuFile);
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
                    return x.EqualsTo(y);
                }

                return x.Id == y.Id; //todo: sb
            }

            public int GetHashCode(Beatmap obj)
            {
                return obj.GetHashCode();
            }
        }
    }

    public class JoinedBeatmap : Beatmap
    {
        public string FileSize { get; set; }
        public string ExportTime { get; set; }
        public string ExportFile { get; set; }
    }
}
