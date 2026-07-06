using System.Diagnostics;

namespace OsuPlayer.Iidx.Abstractions.Charts;

/// <summary>
/// A time-signature change on the chart timeline.
/// </summary>
[DebuggerDisplay("{ToDebuggerDisplay()}")]
public sealed class IidxMeterChange
{
    public IidxMeterChange(int offset, int numerator, int denominator)
    {
        Offset = offset;
        Numerator = numerator;
        Denominator = denominator;
    }

    public int Offset { get; set; }
    public int Numerator { get; set; }
    public int Denominator { get; set; }

    private string ToDebuggerDisplay() => $"{Offset}: {Numerator}/{Denominator}";
}