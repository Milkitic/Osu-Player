using System.Diagnostics;

namespace OsuPlayer.Iidx.Abstractions.Charts;

/// <summary>
/// A change of the active sample for a lane at a given offset. Subsequent
/// <see cref="IidxNote"/>s on the same lane reference this change.
/// </summary>
[DebuggerDisplay("{ToDebuggerDisplay()}")]
public sealed class IidxSampleChange
{
    public IidxSampleChange(int startOffset, int laneIndex, int sampleId)
    {
        StartOffset = startOffset;
        LaneIndex = laneIndex;
        SampleId = sampleId;
    }

    public int StartOffset { get; set; }
    public int LaneIndex { get; set; }
    public int SampleId { get; set; }

    private string ToDebuggerDisplay() => $"{StartOffset}[{LaneIndex}]: #{SampleId:D4}";
}