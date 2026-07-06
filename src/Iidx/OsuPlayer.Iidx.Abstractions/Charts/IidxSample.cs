using System.Diagnostics;

namespace OsuPlayer.Iidx.Abstractions.Charts;

/// <summary>
/// A BGM sample trigger: plays a one-shot background sample at <see cref="Offset"/>
/// with stereo panning <see cref="Stereo"/>.
/// </summary>
[DebuggerDisplay("{ToDebuggerDisplay()}")]
public sealed class IidxSample
{
    public IidxSample(int offset, int stereo, int sampleId)
    {
        Offset = offset == 0 ? 0x8 : offset;
        Stereo = stereo;
        SampleId = sampleId;
    }

    public int Offset { get; set; }

    /// <summary>
    /// Stereo panning (01-0F, left to right, 08 is center).
    /// </summary>
    public int Stereo { get; set; }

    public int SampleId { get; set; }

    private string ToDebuggerDisplay() => $"{Offset}: {Stereo:X1} #{SampleId:D4}";
}