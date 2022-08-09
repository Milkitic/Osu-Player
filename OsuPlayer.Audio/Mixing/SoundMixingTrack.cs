using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coosu.Beatmap.Extensions.Playback;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.OsuPlayer.Audio.Mixing;

public class SoundMixingTrack : Track
{
    private readonly MixingSettings _mixingSettings;
    private readonly WaveFormat _waveFormat;
    private readonly LoopProviders _loopProviders;

    private MixingSampleProvider? _mixingSampleProvider;
    private EnhancedVolumeSampleProvider? _volumeProvider;
    private Queue<HitsoundNode>? _hitsoundQueue;
    private bool _rebuildRequested;
    private bool _isPlayReady;

    public SoundMixingTrack(MixingSettings mixingSettings, TimerSource timerSource,
        List<HitsoundNode>? hitsoundNodes = null)
        : base(timerSource)
    {
        HitsoundNodes = hitsoundNodes ?? new List<HitsoundNode>();

        _mixingSettings = mixingSettings;
        _waveFormat = mixingSettings.WaveFormat;
        _loopProviders = new LoopProviders();
    }

    public List<HitsoundNode> HitsoundNodes { get; }

    public void RebuildSoundElementQueue()
    {
        var currentTime = TimerSource.ElapsedMilliseconds;
        var queue = new Queue<HitsoundNode>();
        foreach (var hitsoundNode in HitsoundNodes.OrderBy(k => k.Offset))
        {
            if (hitsoundNode.Offset < currentTime)
                continue;
            queue.Enqueue(hitsoundNode);
        }

        _hitsoundQueue = queue;
    }

    public override ValueTask DisposeAsync()
    {
        _loopProviders.RemoveAll(_mixingSampleProvider);
        return ValueTask.CompletedTask;
    }

    public override void OnUpdated(double previous, double current)
    {
        if (!_isPlayReady) return;

        if (_rebuildRequested)
        {
            _rebuildRequested = false;
            RebuildSoundElementQueue();
        }
        else if (current < previous || current - previous > 1000) // Force to rebuild queue
        {
            RebuildSoundElementQueue();
        }

        var hitsoundQueue = _hitsoundQueue;
        var rootMixer = _mixingSampleProvider;
        if (rootMixer != null && hitsoundQueue != null)
        {
            while (hitsoundQueue.TryPeek(out var hitsoundNode) && hitsoundNode.Offset <= current)
            {
                PlayHitsoundNode(hitsoundQueue.Dequeue(), rootMixer);
            }
        }
    }

    public override void OnStatusChanged(TimerStatus previousStatus, TimerStatus currentStatus)
    {
        if (currentStatus is TimerStatus.Reset or TimerStatus.Restart or TimerStatus.Skip)
        {
            _rebuildRequested = true;
        }

        if (currentStatus is TimerStatus.Stop or TimerStatus.Reset)
        {
            _isPlayReady = false;
        }
        else if (currentStatus is TimerStatus.Start or TimerStatus.Restart)
        {
            _isPlayReady = true;
        }
    }

    protected override async ValueTask InitializeCoreAsync()
    {
        _mixingSampleProvider = new MixingSampleProvider(_waveFormat)
        {
            ReadFully = true
        };
        _volumeProvider = new EnhancedVolumeSampleProvider(_mixingSampleProvider);
        RootSampleProvider = _volumeProvider;
        Duration = HitsoundNodes is { Count: not 0 } ? HitsoundNodes.Max(k => k.Offset) : 0;
        RebuildSoundElementQueue();
        await InitializeActualDurationAsync();
        //var hitsoundNodes = HitsoundNodes;
        //if (hitsoundNodes != null)
        //{
        //    await Task.Run(() =>
        //    {
        //        hitsoundNodes
        //            .Where(k => k.Filename != null)
        //            .TakeLast(9)
        //            .AsParallel()
        //            .Select(k => CachedSoundFactory.GetOrCreateCacheSound(_waveFormat, k.Filename!));
        //    });
        //}
    }

    protected virtual ValueTask InitializeActualDurationAsync()
    {
        return ValueTask.CompletedTask;
    }

    private void PlayHitsoundNode(HitsoundNode hitsoundNode, MixingSampleProvider rootMixer)
    {
        if (hitsoundNode is PlayableNode playableNode)
        {
            if (!CacheManager.Instance.TryGetAudioByNode(playableNode, out var cachedSound)) return;
            var volume = playableNode.Volume;
            var balance = playableNode.Balance;
            rootMixer.AddMixerInput(
                new BalanceSampleProvider(
                        new EnhancedVolumeSampleProvider(
                                new SeekableCachedSoundSampleProvider(cachedSound!))
                        { Volume = volume }
                    )
                { Balance = balance }
            );
        }
        else if (hitsoundNode is ControlNode controlNode)
        {
            var volume = controlNode.Volume;
            var balance = controlNode.Balance;
            if (controlNode.ControlType == ControlType.StartSliding)
            {
                if (_loopProviders.ShouldRemoveAll(controlNode.SlideChannel))
                {
                    _loopProviders.RemoveAll(rootMixer);
                }

                if (CacheManager.Instance.TryGetAudioByNode(controlNode, out var cachedSound))
                {
                    _loopProviders.Create(controlNode, cachedSound!, rootMixer, volume, balance);
                }
            }
            else if (controlNode.ControlType == ControlType.StopSliding)
            {
                _loopProviders.Remove(controlNode.SlideChannel, rootMixer);
            }
            else if (controlNode.ControlType == ControlType.ChangeVolume)
            {
                _loopProviders.ChangeAllVolumes(volume);
            }
            else if (controlNode.ControlType == ControlType.ChangeBalance)
            {
                _loopProviders.ChangeAllBalances(balance);
            }
        }
    }
}

public class MixingSettings
{
    public MixingSettings(WaveFormat waveFormat)
    {
        WaveFormat = waveFormat;
    }

    public WaveFormat WaveFormat { get; }
}