using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Anotar.NLog;
using Coosu.Beatmap.Extensions.Playback;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.OsuPlayer.Audio.Mixing;

public class SoundMixingTrack : Track
{
    private readonly WaveFormat _waveFormat;
    private readonly LoopProviders _loopProviders;

    private MixingSampleProvider? _mixingSampleProvider;
    private EnhancedVolumeSampleProvider? _volumeProvider;
    private Queue<HitsoundNode>? _hitsoundQueue;
    private bool _rebuildRequested;
    private bool _isPlayReady;

    public SoundMixingTrack(TimerSource timerSource, WaveFormat waveFormat,
        List<HitsoundNode>? hitsoundNodes = null)
        : base(timerSource)
    {
        HitsoundNodes = hitsoundNodes ?? new List<HitsoundNode>();
        _waveFormat = waveFormat;
        _loopProviders = new LoopProviders();
    }

    public float BalanceRatio { get; set; } = 0.3f;
    public List<HitsoundNode> HitsoundNodes { get; set; }

    public void RebuildSoundElementQueue()
    {
        var currentTime = TimerSource.ElapsedMilliseconds - Offset;
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
        await InitializeHitsoundAsync();
    }

    private async Task InitializeHitsoundAsync()
    {
        Duration = HitsoundNodes is { Count: not 0 } ? HitsoundNodes.Max(k => k.Offset) : 0;
        RebuildSoundElementQueue();
        await InitializeActualDurationAsync();
    }

    protected virtual ValueTask InitializeActualDurationAsync()
    {
        return ValueTask.CompletedTask;
    }

    private void PlayHitsoundNode(HitsoundNode hitsoundNode, MixingSampleProvider rootMixer)
    {
        if (hitsoundNode is PlayableNode playableNode)
        {
            if (!CacheManager.Instance.TryGetAudioByNode(playableNode, out var cachedSound))
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
                if (_loopProviders.ShouldRemoveAll(controlNode.SlideChannel))
                {
                    _loopProviders.RemoveAll(rootMixer);
                }

                if (!CacheManager.Instance.TryGetAudioByNode(controlNode, out var cachedSound))
                {
                    LogTo.Warn("Failed to find CachedSound to play ControlNode:" + DebuggerDisplay(hitsoundNode));
                    return;
                }

                _loopProviders.Create(controlNode, cachedSound!, rootMixer, volume, balance);
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
}