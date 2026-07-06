using System.Diagnostics;

namespace OsuPlayer.Iidx.Abstractions.Charts;

/// <summary>
/// A BPM change point on the chart timeline.
/// </summary>
[DebuggerDisplay("{ToDebuggerDisplay()}")]
public sealed class IidxTimingPoint
{
    public IidxTimingPoint(int offset, int bpm)
    {
        Offset = offset;
        Bpm = bpm;
    }

    public int Offset { get; set; }
    public int Bpm { get; set; }

    private string ToDebuggerDisplay() => $"{Offset}: {Bpm} BPM";
}