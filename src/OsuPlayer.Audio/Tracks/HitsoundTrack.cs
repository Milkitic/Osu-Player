using Coosu.Beatmap.Extensions.Playback;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.OsuPlayer.Audio.Mixing;
using NAudio.Wave;

namespace Milki.OsuPlayer.Audio.Tracks;

public class HitsoundTrack : SoundMixingTrack
{
    private readonly WaveFormat _waveFormat;

    public HitsoundTrack(TimerSource timerSource, WaveFormat waveFormat, List<HitsoundNode>? hitsoundNodes = null)
        : base(timerSource, waveFormat, hitsoundNodes)
    {
        _waveFormat = waveFormat;
    }

    protected override async ValueTask InitializeActualDurationAsync()
    {
        var hitsoundNodes = HitsoundNodes;
        if (hitsoundNodes.Count == 0) return;
        var last9Elements = await Task.Run(() =>
        {
            return hitsoundNodes
                .Where(k => k.Filename != null)
                .TakeLast(9)
                .AsParallel()
                .Select(k => (node: k,
                    cache: CachedSoundFactory.GetOrCreateCacheSound(_waveFormat, k.Filename!, useWdlResampler: true)
                        .Result))
                .ToArray();
        });

        var max = last9Elements.Length == 0
            ? 0
            : last9Elements.Max(k =>
            {
                var (node, cache) = k;
                return node.Offset + cache?.Duration.TotalMilliseconds ?? 0;
            });

        Duration = Math.Max(Duration, max);
    }
}