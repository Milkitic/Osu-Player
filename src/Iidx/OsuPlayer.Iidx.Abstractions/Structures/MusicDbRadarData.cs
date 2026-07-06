using System.Runtime.InteropServices;

namespace OsuPlayer.Iidx.Abstractions.Structures;

/// <summary>
/// IIDX <c>music_data.bin</c> radar data block: one per difficulty slot.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MusicDbRadarData
{
    public int Notes;
    public int Peak;
    public int Scratch;
    public int Soflan;
    public int Charge;
    public int Chord;
}