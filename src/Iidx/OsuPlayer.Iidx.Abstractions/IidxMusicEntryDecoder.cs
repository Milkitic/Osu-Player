using OsuPlayer.Iidx.Abstractions.Structures;

namespace OsuPlayer.Iidx.Abstractions;

/// <summary>
/// Static helpers that project the on-disk <see cref="MusicDbEntry32"/> struct
/// into the platform-agnostic <see cref="IidxMusicEntry"/>. Keeps string
/// decoding (UTF-16 for title/genre/artist, Shift-JIS for romanized/filenames)
/// in one place so the database layer never touches the raw struct.
/// </summary>
public static class IidxMusicEntryDecoder
{
    public static IidxMusicEntry ToMusicEntry(in MusicDbEntry32 entry)
    {
        var radar = new IidxRadarData[10];
        radar[0] = ToRadar(entry.RadarSPB);
        radar[1] = ToRadar(entry.RadarSPN);
        radar[2] = ToRadar(entry.RadarSPH);
        radar[3] = ToRadar(entry.RadarSPA);
        radar[4] = ToRadar(entry.RadarSPL);
        radar[5] = ToRadar(entry.RadarDPB);
        radar[6] = ToRadar(entry.RadarDPN);
        radar[7] = ToRadar(entry.RadarDPH);
        radar[8] = ToRadar(entry.RadarDPA);
        radar[9] = ToRadar(entry.RadarDPL);

        var levels = new byte[10];
        levels[0] = entry.LvSPB;
        levels[1] = entry.LvSPN;
        levels[2] = entry.LvSPH;
        levels[3] = entry.LvSPA;
        levels[4] = entry.LvSPL;
        levels[5] = entry.LvDPB;
        levels[6] = entry.LvDPN;
        levels[7] = entry.LvDPH;
        levels[8] = entry.LvDPA;
        levels[9] = entry.LvDPL;

        var notes = new int[10];
        notes[0] = entry.NotesCountSPB;
        notes[1] = entry.NotesCountSPN;
        notes[2] = entry.NotesCountSPH;
        notes[3] = entry.NotesCountSPA;
        notes[4] = entry.NotesCountSPL;
        notes[5] = entry.NotesCountDPB;
        notes[6] = entry.NotesCountDPN;
        notes[7] = entry.NotesCountDPH;
        notes[8] = entry.NotesCountDPA;
        notes[9] = entry.NotesCountDPL;

        var files = new byte[10];
        files[0] = entry.FileIdentifierSPB;
        files[1] = entry.FileIdentifierSPN;
        files[2] = entry.FileIdentifierSPH;
        files[3] = entry.FileIdentifierSPA;
        files[4] = entry.FileIdentifierSPL;
        files[5] = entry.FileIdentifierDPB;
        files[6] = entry.FileIdentifierDPN;
        files[7] = entry.FileIdentifierDPH;
        files[8] = entry.FileIdentifierDPA;
        files[9] = entry.FileIdentifierDPL;

        return new IidxMusicEntry
        {
            MusicId = entry.musicId,
            Title = entry.Title,
            TitleRoman = entry.TitleRoman,
            Genre = entry.Genre,
            Artist = entry.Artist,
            License = entry.License,
            BgaFilename = entry.BgaFilename,
            Version = entry.Version,
            OtherFolder = entry.OtherFolder,
            BemaniFolder = entry.BemaniFolder,
            SwitchableDiff = entry.SwitchableDiff,
            DifficultyLevels = levels,
            NoteCounts = notes,
            FileIdentifiers = files,
            RadarData = radar,
            BgmVolume = entry.bgmVolume,
            BgaDelay = entry.BgaDelay,
            TitleFontType = entry.TitleFontType,
            TitleImg = entry.TitleImg != 0,
            ArtistImg = entry.ArtistImg != 0,
            GenreImg = entry.GenreImg != 0,
            BannerImg = entry.BannerImg != 0,
            PrepareSceneTitleImg = entry.PrepareSceneTitleImg != 0,
            LayersFlag = entry.LayersFlag
        };
    }

    private static IidxRadarData ToRadar(in MusicDbRadarData data) =>
        new(data.Notes, data.Peak, data.Scratch, data.Soflan, data.Charge, data.Chord);
}
