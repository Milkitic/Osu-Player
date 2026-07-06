using System.Diagnostics;

namespace OsuPlayer.Iidx.Abstractions.Charts;

/// <summary>
/// A single playable note on an IIDX chart lane.
/// </summary>
/// <remarks>
/// Ported from <c>IIDXToolbox.Readers.Charts.Note</c>. Lane semantics are
/// 0..6 = keys, 7 = scratch (per player side). Duration &gt; 0 indicates a
/// long note (charge note / back-spin).
/// </remarks>
[DebuggerDisplay("{ToDebuggerDisplay()}")]
public sealed class IidxNote
{
    public IidxNote(int laneIndex, int offset, int duration, IidxSampleChange sampleChange)
    {
        LaneIndex = laneIndex;
        Offset = offset;
        Duration = duration;
        SampleChange = sampleChange;
    }

    /// <summary>
    /// 0-6 keys, 7 scratch. Player 2 lanes are offset by 8 by the chart parser.
    /// </summary>
    public int LaneIndex { get; set; }

    public int Offset { get; set; }
    public int Duration { get; set; }
    public IidxSampleChange SampleChange { get; set; }

    private string ToDebuggerDisplay() => Duration > 0
        ? $"{Offset}[{LaneIndex}]: ~{Offset + Duration} #{SampleChange.SampleId:D4}"
        : $"{Offset}[{LaneIndex}]: #{SampleChange.SampleId:D4}";
}