using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OsuPlayer.Iidx.Abstractions.Internal;

namespace OsuPlayer.Iidx.Abstractions.Structures;

/// <summary>
/// On-disk <c>music_data.bin</c> entry. Layout is fixed and must not change.
/// </summary>
/// <remarks>
/// Ported verbatim from <c>IIDXToolbox.Readers.Structures.MusicDbEntry32</c>.
/// String fields are decoded by <see cref="IidxMusicEntryDecoder"/>.
/// </remarks>
[DebuggerDisplay("[{musicId}] {Artist} - {Title}")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct MusicDbEntry32
{
    public const int TitleSize = 256;
    public const int TitleRomanSize = 64;
    public const int GenreSize = 128;
    public const int ArtistSize = 256;
    public const int LicenseSize = 256;
    public const int BgaFilenameSize = 32;
    public const int LayersFlagSize = 320;

    private fixed byte _pTitle[TitleSize];       // 歌名 (UTF-16)
    private fixed byte _pTitleRoman[TitleRomanSize]; // 歌名罗马字 (Shift-JIS)
    private fixed byte _pGenre[GenreSize];       // 曲风 (UTF-16)
    private fixed byte _pArtist[ArtistSize];     // 作曲家 (UTF-16)
    private fixed byte _pLicense[LicenseSize];

    public int TitleImg;
    public int ArtistImg;
    public int GenreImg;
    public int PrepareSceneTitleImg;
    public int BannerImg;
    private int _unknown0X03d4;
    public int TitleFontType;

    public short Version;          // signed; omni uses -1
    public short OtherFolder;
    public short BemaniFolder;
    private short _unknown0X03E2;
    private short _unknown0X03E4;
    private short _unknown0X03E6;
    public short SwitchableDiff;
    private short _unknown0X03Ea;

    public byte LvSPB;
    public byte LvSPN;
    public byte LvSPH;
    public byte LvSPA;
    public byte LvSPL;
    public byte LvDPB;
    public byte LvDPN;
    public byte LvDPH;
    public byte LvDPA;
    public byte LvDPL;

    private fixed byte _unknown1[6];
    public fixed byte BPM[0x50];
    private fixed byte _unknown2[0x30];

    public int NotesCountSPB;
    public int NotesCountSPN;
    public int NotesCountSPH;
    public int NotesCountSPA;
    public int NotesCountSPL;
    public int NotesCountDPB;
    public int NotesCountDPN;
    public int NotesCountDPH;
    public int NotesCountDPA;
    public int NotesCountDPL;

    private fixed byte _unknown3[0x58];

    public MusicDbRadarData RadarSPB;
    public MusicDbRadarData RadarSPN;
    public MusicDbRadarData RadarSPH;
    public MusicDbRadarData RadarSPA;
    public MusicDbRadarData RadarSPL;
    public MusicDbRadarData RadarDPB;
    public MusicDbRadarData RadarDPN;
    public MusicDbRadarData RadarDPH;
    public MusicDbRadarData RadarDPA;
    public MusicDbRadarData RadarDPL;

    private fixed byte _unknown4[0x90];

    public int musicId;
    public int bgmVolume;

    public byte FileIdentifierSPB;
    public byte FileIdentifierSPN;
    public byte FileIdentifierSPH;
    public byte FileIdentifierSPA;
    public byte FileIdentifierSPL;
    public byte FileIdentifierDPB;
    public byte FileIdentifierDPN;
    public byte FileIdentifierDPH;
    public byte FileIdentifierDPA;
    public byte FileIdentifierDPL;

    public short BgaDelay;
    private fixed byte _pBgaFilename[BgaFilenameSize];

    public int LayersEnabled;
    private fixed byte _pLayersFlag[LayersFlagSize];

    private int _unknownTail;

    public string Title
    {
        get
        {
            fixed (byte* p = _pTitle)
            {
                return Marshal.PtrToStringUni((IntPtr)p) ?? string.Empty;
            }
        }
    }

    public string TitleRoman => ReadSjis(ref _pTitleRoman[0], TitleRomanSize);

    public string Genre
    {
        get
        {
            fixed (byte* p = _pGenre)
            {
                return Marshal.PtrToStringUni((IntPtr)p) ?? string.Empty;
            }
        }
    }

    public string Artist
    {
        get
        {
            fixed (byte* p = _pArtist)
            {
                return Marshal.PtrToStringUni((IntPtr)p) ?? string.Empty;
            }
        }
    }

    public string License
    {
        get
        {
            fixed (byte* p = _pLicense)
            {
                return Marshal.PtrToStringUni((IntPtr)p) ?? string.Empty;
            }
        }
    }

    public string BgaFilename => ReadSjis(ref _pBgaFilename[0], BgaFilenameSize);

    public string[]? LayersFlag
    {
        get
        {
            if (LayersEnabled == 0) return null;

            var span = MemoryMarshal.CreateReadOnlySpan(ref _pLayersFlag[0], LayersFlagSize);
            var array = new string[10];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = ReadSjis(span.Slice(i * 32, 32));
            }

            return array;
        }
    }

    private static string ReadSjis(ref byte source, int size) =>
        ReadSjis(MemoryMarshal.CreateReadOnlySpan(ref source, size));

    private static string ReadSjis(ReadOnlySpan<byte> span)
    {
        var nullIndex = span.IndexOf((byte)0);
        if (nullIndex >= 0)
        {
            span = span[..nullIndex];
        }

        return SjisEncoding.Instance.GetString(span);
    }
}
