using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Milky.OsuPlayer.Data;
using Newtonsoft.Json;
using osu.Shared;
using osu_database_reader.Components.Beatmaps;

namespace Milky.OsuPlayer.Common.Data.EF.Model
{
    public class Beatmap : IMapIdentifiable
    {
        public string Artist { get; set; }
        public string ArtistUnicode { get; set; }
        public string Title { get; set; }
        public string TitleUnicode { get; set; }
        public string Creator { get; set; }  //mapper
        public string Version { get; set; }  //difficulty name
        public string BeatmapFileName { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public double DiffSrNoneStandard { get; set; }
        public double DiffSrNoneTaiko { get; set; }
        public double DiffSrNoneCtB { get; set; }
        public double DiffSrNoneMania { get; set; }
        public int DrainTimeSeconds { get; set; }    //NOTE: in s
        public int TotalTime { get; set; }           //NOTE: in ms
        public int AudioPreviewTime { get; set; }    //NOTE: in ms
        public int BeatmapId { get; set; }
        public int BeatmapSetId { get; set; }
        public OSharp.Beatmap.Sections.GamePlay.GameMode GameMode { get; set; }
        public string SongSource { get; set; }
        public string SongTags { get; set; }
        public string FolderName { get; set; }

        [Required, Column("id")]
        [JsonProperty("id")]
        public Guid Id { get; set; }
        
        #region Only used in HoLLy

        //public string AudioFileName { get; set; }
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
            DiffSrNoneStandard = entry.DiffStarRatingStandard[Mods.None];
            DiffSrNoneTaiko = entry.DiffStarRatingTaiko[Mods.None];
            DiffSrNoneCtB = entry.DiffStarRatingCtB[Mods.None];
            DiffSrNoneMania = entry.DiffStarRatingMania[Mods.None];
            DrainTimeSeconds = entry.DrainTimeSeconds;
            TotalTime = entry.TotalTime;
            AudioPreviewTime = entry.AudioPreviewTime;
            BeatmapId = entry.BeatmapId;
            BeatmapSetId = entry.BeatmapSetId;
            GameMode = entry.GameMode.ParseHollyToOSharp();
            SongSource = entry.SongSource;
            SongTags = entry.SongTags;
            FolderName = entry.FolderName;

            return this;
        }

        public static Beatmap ParseFromHolly(BeatmapEntry entry)
        {
            return (new Beatmap()).UpdateFromHolly(entry);
        }
    }

    public static class EnumExt
    {
        public static OSharp.Beatmap.Sections.GamePlay.GameMode ParseHollyToOSharp(this osu.Shared.GameMode gameMode)
        {
            return (OSharp.Beatmap.Sections.GamePlay.GameMode)(int)gameMode;

            #region not sure

            //switch (gameMode)
            //{
            //    case osu.Shared.GameMode.Standard:
            //        return OSharp.Beatmap.Sections.GamePlay.GameMode.Circle;
            //    case osu.Shared.GameMode.Taiko:
            //        return OSharp.Beatmap.Sections.GamePlay.GameMode.Taiko;
            //    case osu.Shared.GameMode.CatchTheBeat:
            //        return OSharp.Beatmap.Sections.GamePlay.GameMode.Catch;
            //    case osu.Shared.GameMode.Mania:
            //        return OSharp.Beatmap.Sections.GamePlay.GameMode.Mania;
            //    default:
            //        throw new ArgumentOutOfRangeException(nameof(gameMode), gameMode, null);
            //}

            #endregion
        }
    }
}
