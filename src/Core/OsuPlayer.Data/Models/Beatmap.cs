using System;
using System.Collections.Generic;
using Coosu.Beatmap;
using Coosu.Database.DataTypes;
using Dapper.FluentMap.Mapping;
using OsuPlayer.Shared;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Data.Models;

public class BeatmapMap : EntityMap<Beatmap>
{
    public BeatmapMap()
    {
        Map(p => p.Artist).ToColumn("artist");
        Map(p => p.Title).ToColumn("title");
        Map(p => p.ArtistUnicode).ToColumn("artist_unicode");
        Map(p => p.TitleUnicode).ToColumn("title_unicode");
        Map(p => p.Creator).ToColumn("creator");
        Map(p => p.BeatmapFileName).ToColumn("beatmap_file_name");
        Map(p => p.LastModifiedTime).ToColumn("last_modified_at");
        Map(p => p.DiffSrNoneStandard).ToColumn("star_rating_standard");
        Map(p => p.DiffSrNoneTaiko).ToColumn("star_rating_taiko");
        Map(p => p.DiffSrNoneCtB).ToColumn("star_rating_catch");
        Map(p => p.DiffSrNoneMania).ToColumn("star_rating_mania");
        Map(p => p.DrainTimeSeconds).ToColumn("drain_time_seconds");
        Map(p => p.TotalTime).ToColumn("total_time_ms");
        Map(p => p.AudioPreviewTime).ToColumn("preview_time_ms");
        Map(p => p.BeatmapId).ToColumn("osu_beatmap_id");
        Map(p => p.BeatmapSetId).ToColumn("osu_beatmapset_id");
        Map(p => p.GameMode).ToColumn("game_mode");
        Map(p => p.SongSource).ToColumn("source");
        Map(p => p.SongTags).ToColumn("tags");
        Map(p => p.FolderName).ToColumn("folder_name");
        Map(p => p.AudioFileName).ToColumn("audio_file_name");
        Map(p => p.Id).ToColumn("id");
        Map(p => p.InOwnDb).ToColumn("is_local");
        Map(p => p.Version).ToColumn("difficulty_name");
        Map(p => p.SourceGame).ToColumn("source_game");
        Map(p => p.IidxMusicId).ToColumn("iidx_music_id");
        Map(p => p.IidxFileIdentifier).ToColumn("iidx_file_identifier");
        Map(p => p.IidxBgmVolume).ToColumn("iidx_bgm_volume");
        Map(p => p.IidxBgaDelay).ToColumn("iidx_bga_delay");
        Map(p => p.IidxVersion).ToColumn("iidx_version");
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

    public Dictionary<Mods, float> StarRatingStd
    {
        get;
        set => field = value ?? new Dictionary<Mods, float>();
    } = new();

    public Dictionary<Mods, float> StarRatingTaiko
    {
        get;
        set => field = value ?? new Dictionary<Mods, float>();
    } = new();

    public Dictionary<Mods, float> StarRatingCtb
    {
        get;
        set => field = value ?? new Dictionary<Mods, float>();
    } = new();

    public Dictionary<Mods, float> StarRatingMania
    {
        get;
        set => field = value ?? new Dictionary<Mods, float>();
    } = new();

    public double DiffSrNoneStandard
    {
        get => StarRatingStd.GetValueOrDefault(Mods.None);
        set => StarRatingStd[Mods.None] = (float)value;
    }

    public double DiffSrNoneTaiko
    {
        get => StarRatingTaiko.GetValueOrDefault(Mods.None);
        set => StarRatingTaiko[Mods.None] = (float)value;
    }

    public double DiffSrNoneCtB
    {
        get => StarRatingCtb.GetValueOrDefault(Mods.None);
        set => StarRatingCtb[Mods.None] = (float)value;
    }

    public double DiffSrNoneMania
    {
        get => StarRatingMania.GetValueOrDefault(Mods.None);
        set => StarRatingMania[Mods.None] = (float)value;
    }

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
    public SourceGame SourceGame { get; set; } = SourceGame.Osu;

    /// <summary>
    /// IIDX-specific: music_id from music_data.bin. 0 for osu! entries.
    /// </summary>
    public int IidxMusicId { get; set; }

    /// <summary>
    /// IIDX-specific: 2dx file identifier per difficulty (e.g. SPB/SPN/.../DPL). 0 for osu! entries.
    /// </summary>
    public byte IidxFileIdentifier { get; set; }

    /// <summary>
    /// IIDX-specific: BGM volume override (0x00-0xFF). Null leaves the engine default.
    /// </summary>
    public int? IidxBgmVolume { get; set; }

    /// <summary>
    /// IIDX-specific: BGA delay in milliseconds. Null for osu! entries.
    /// </summary>
    public short? IidxBgaDelay { get; set; }

    /// <summary>
    /// IIDX-specific: game version the entry belongs to (signed short, -1 for omni). Null for osu!.
    /// </summary>
    public short? IidxVersion { get; set; }

    public string AutoTitle => MetaString.GetUnicode(Title, TitleUnicode) ?? "未知标题";
    public string AutoArtist => MetaString.GetUnicode(Artist, ArtistUnicode) ?? "未知艺术家";

    public override int GetHashCode()
    {
        return HashCode.Combine(SourceGame, FolderName, Version, InOwnDb);
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
        return other != null &&
               SourceGame == other.SourceGame &&
               FolderName == other.FolderName &&
               Version == other.Version &&
               InOwnDb == other.InOwnDb;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Beatmap)obj);
    }
}
