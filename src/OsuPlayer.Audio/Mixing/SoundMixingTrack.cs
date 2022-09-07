using System.Collections.Concurrent;
using Anotar.NLog;
using Coosu.Beatmap.Extensions.Playback;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.OsuPlayer.Audio.Mixing;

public class SoundMixingTrack : Track
{
    private readonly LoopProviderHelper _loopProviderHelper;
    private readonly WaveFormat _waveFormat;
    private ConcurrentQueue<HitsoundNode>? _hitsoundQueue;
    private bool _isPlayReady;

    private MixingSampleProvider? _mixingSampleProvider;
    private bool _rebuildRequested;
    private EnhancedVolumeSampleProvider? _volumeProvider;

    public SoundMixingTrack(TimerSource timerSource, WaveFormat waveFormat,
        List<HitsoundNode>? hitsoundNodes = null)
        : base(timerSource)
    {
        HitsoundNodes = hitsoundNodes ?? new List<HitsoundNode>();
        _waveFormat = waveFormat;
        _loopProviderHelper = new LoopProviderHelper();
    }

    public float BalanceRatio { get; set; } = 0.3f;
    public List<HitsoundNode> HitsoundNodes { get; set; }

    public void RebuildNodeQueue()
    {
        var queue = RebuildNodeQueueCore();
        _hitsoundQueue = queue;
    }

    public override ValueTask DisposeAsync()
    {
        _loopProviderHelper.RemoveAll(_mixingSampleProvider);
        return ValueTask.CompletedTask;
    }

    public override void OnUpdated(double previous, double current)
    {
        if (!_isPlayReady) return;

        if (_rebuildRequested)
        {
            _rebuildRequested = false;
            RebuildNodeQueue();
        }
        else if (current < previous || current - previous > 1000) // Force to rebuild queue
        {
            RebuildNodeQueue();
        }

        var hitsoundQueue = _hitsoundQueue;
        var rootMixer = _mixingSampleProvider;
        if (rootMixer == null || hitsoundQueue == null) return;
        while (hitsoundQueue.TryPeek(out var hitsoundNode) && hitsoundNode.Offset <= current)
        {
            if (hitsoundQueue.TryDequeue(out hitsoundNode))
            {
                PlayHitsoundNode(hitsoundNode, rootMixer);
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

    public string DebuggerDisplay(HitsoundNode node)
    {
        if (node is PlayableNode playableNode)
        {
            return $"PL{(playableNode.UseUserSkin ? "D" : "")}:{Offset}: " +
                   $"P{(int)playableNode.PlayablePriority}: " +
                   $"V{(playableNode.Volume * 10):#.#}: " +
                   $"B{(playableNode.Balance * 10):#.#}: " +
                   $"{(playableNode.Filename)}";
        }

        if (node is ControlNode controlNode)
        {
            return $"CT{(controlNode.UseUserSkin ? "D" : "")}:{Offset}: " +
                   $"O{Offset}: " +
                   $"T{(int)controlNode.ControlType}{(controlNode.ControlType is ControlType.StartSliding or ControlType.StopSliding ? (int)controlNode.SlideChannel : "")}: " +
                   $"V{(controlNode.Volume * 10):#.#}: " +
                   $"B{(controlNode.Balance * 10):#.#}: " +
                   $"{(controlNode.Filename == null ? "" : Path.GetFileNameWithoutExtension(controlNode.Filename))}";
        }

        return "";
    }

    protected virtual ConcurrentQueue<HitsoundNode> RebuildNodeQueueCore()
    {
        var currentTime = TimerSource.ElapsedMilliseconds - Offset;
        var queue = new ConcurrentQueue<HitsoundNode>();
        foreach (var hitsoundNode in HitsoundNodes.OrderBy(k => k.Offset))
        {
            if (hitsoundNode.Offset < currentTime)
                continue;
            queue.Enqueue(hitsoundNode);
        }

        return queue;
    }

    protected override async ValueTask InitializeCoreAsync()
    {
        _mixingSampleProvider = new MixingSampleProvider(_waveFormat)
        {
            ReadFully = true
        };
        _volumeProvider = new EnhancedVolumeSampleProvider(_mixingSampleProvider);
        RootSampleProvider = _volumeProvider;
        await InitializeHitsoundAsync();
    }

    protected virtual ValueTask InitializeActualDurationAsync()
    {
        return ValueTask.CompletedTask;
    }

    private async ValueTask InitializeHitsoundAsync()
    {
        Duration = HitsoundNodes is { Count: not 0 } ? HitsoundNodes.Max(k => k.Offset) : 0;
        RebuildNodeQueue();
        await InitializeActualDurationAsync();
    }

    private void PlayHitsoundNode(HitsoundNode hitsoundNode, MixingSampleProvider rootMixer)
    {
        if (hitsoundNode is PlayableNode playableNode)
        {
            if (!AudioCacheManager.Instance.TryGetAudioByNode(playableNode, out var cachedSound))
            {
                LogTo.Warn("Failed to find CachedSound to play PlayableNode:" + DebuggerDisplay(hitsoundNode));
                return;
            }

            var volume = playableNode.Volume;
            var balance = playableNode.Balance * BalanceRatio;
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
                if (_loopProviderHelper.ShouldRemoveAll(controlNode.SlideChannel))
                {
                    _loopProviderHelper.RemoveAll(rootMixer);
                }

                if (!AudioCacheManager.Instance.TryGetAudioByNode(controlNode, out var cachedSound))
                {
                    LogTo.Warn("Failed to find CachedSound to play ControlNode:" + DebuggerDisplay(hitsoundNode));
                    return;
                }

                _loopProviderHelper.Create(controlNode, cachedSound!, rootMixer, volume, balance);
            }
            else if (controlNode.ControlType == ControlType.StopSliding)
            {
                _loopProviderHelper.Remove(controlNode.SlideChannel, rootMixer);
            }
            else if (controlNode.ControlType == ControlType.ChangeVolume)
            {
                _loopProviderHelper.ChangeAllVolumes(volume);
            }
            else if (controlNode.ControlType == ControlType.ChangeBalance)
            {
                _loopProviderHelper.ChangeAllBalances(balance);
            }
        }
    }
}