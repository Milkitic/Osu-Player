using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Coosu.Beatmap.Extensions.Playback;
using Milki.OsuPlayer.Audio.Internal;
using Milki.OsuPlayer.Audio.Mixing;
using NAudio.Wave;

namespace Milki.OsuPlayer.Audio.Tracks;

public class SampleTrack : HitsoundTrack
{
    private readonly OsuFile _osuFile;
    private bool _keepTune;

    public SampleTrack(OsuFile osuFile, TimerSource timerSource, WaveFormat waveFormat, List<HitsoundNode>? hitsoundNodes = null)
        : base(timerSource, waveFormat, hitsoundNodes)
    {
        _osuFile = osuFile;
    }

    public TimeSpan MusicTrackDuration { get; set; }

    public override bool KeepTune
    {
        get => _keepTune;
        set
        {
            if (_keepTune == value) return;
            _keepTune = value;
            RebuildNodeQueue();
        }
    }

    public override void OnRateChanged(float previousRate, float currentRate)
    {
        if (Math.Abs(previousRate - 1.5f) < 0.001 ||
            Math.Abs(currentRate - 1.5f) < 0.001)
        {
            RebuildNodeQueue();
        }
    }

    protected override async ValueTask InitializeCoreAsync()
    {
        HitsoundNodes.AddRange(NightcoreTilingHelper.GetHitsoundNodes(_osuFile, MusicTrackDuration));
        await base.InitializeCoreAsync();
    }

    protected override Queue<HitsoundNode> RebuildNodeQueueCore()
    {
        var currentTime = TimerSource.ElapsedMilliseconds - Offset;
        var queue = new Queue<HitsoundNode>();
        IEnumerable<HitsoundNode> hitsoundNodes = HitsoundNodes;

        if (KeepTune || Math.Abs(TimerSource.Rate - 1.5f) >= 0.001)
        {
            hitsoundNodes = hitsoundNodes
                .Where(k => k is PlayableNode
                {
                    PlayablePriority: PlayablePriority.Sampling
                });
        }

        foreach (var hitsoundNode in hitsoundNodes.OrderBy(k => k.Offset))
        {
            if (hitsoundNode.Offset < currentTime)
                continue;
            queue.Enqueue(hitsoundNode);
        }

        return queue;
    }
}