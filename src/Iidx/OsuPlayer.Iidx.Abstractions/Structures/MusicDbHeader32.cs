using System.Runtime.InteropServices;

namespace OsuPlayer.Iidx.Abstractions.Structures;

/// <summary>
/// 32-byte-slot IIDX <c>music_data.bin</c> header. Layout is on-disk and must not change.
/// </summary>
/// <remarks>
/// Ported verbatim from <c>IIDXToolbox.Readers.Structures.MusicDbHeader32</c>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct MusicDbHeader32
{
    public const int HeaderFlagSize = 4;
    public const int SlotSize = 4;

    public fixed byte HeaderFlag[HeaderFlagSize];
    public byte Version;
    private fixed byte _p[3];
    public int SongCount;
    public int SlotCount;
}