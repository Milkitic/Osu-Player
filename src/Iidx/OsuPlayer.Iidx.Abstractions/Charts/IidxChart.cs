using System;
using System.Collections.Generic;
using System.Linq;

namespace OsuPlayer.Iidx.Abstractions.Charts;

/// <summary>
/// Parsed IIDX chart for a single difficulty. Contains the timeline of notes,
/// sample changes, BGM samples, BPM changes and meter changes — everything the
/// audio session needs to schedule playback events.
/// </summary>
/// <remarks>
/// Ported from <c>IIDXToolbox.Readers.Charts.Chart</c>. Immutable after construction;
/// the lists are populated by <c>IidxChartParser</c>.
/// </remarks>
public sealed class IidxChart
{
    public required IidxDifficulty ChartDifficulty { get; init; }

    /// <summary>
    /// 0-6 keys, 7 scratch (per player side; player 2 lanes offset by 8).
    /// </summary>
    public required List<IidxNote> Notes { get; init; }
    public required List<IidxSampleChange> SampleChanges { get; init; }
    public required List<IidxSample> Samples { get; init; }
    public required List<IidxTimingPoint> TimingPoints { get; init; }
    public required List<IidxMeterChange> MeterChanges { get; init; }

    public int MaxSampleId => Notes.Count == 0
        ? 0
        : Math.Max(Samples.Max(x => x.SampleId), Notes.Select(x => x.SampleChange.SampleId).Max());

    public int MinSampleId => Notes.Count == 0
        ? int.MaxValue
        : Math.Min(Samples.Min(x => x.SampleId), Notes.Select(x => x.SampleChange.SampleId).Min());

    public int NoteCount => Notes.Count;

    /// <summary>
    /// Returns the union of BGM samples and note samples, with BGM stereo normalized
    /// to lane 0 (center). Used to enumerate every sample the chart will request.
    /// </summary>
    public HashSet<IidxSample> GetCombinedUniqueSamples()
    {
        var res = Samples
            .Select(x => new IidxSample(x.Offset, x.Stereo == 8 ? 0 : x.Stereo, x.SampleId))
            .ToList();

        res.AddRange(Notes.Select(x => new IidxSample(x.Offset, x.SampleChange.LaneIndex, x.SampleChange.SampleId)));
        return res.ToHashSet();
    }

    public bool HasSpeedChanges => TimingPoints.DistinctBy(x => x.Bpm).Count() > 1;
}