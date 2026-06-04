using System;
using KeyAsio.Core.Audio.Caching;
using KeyAsio.Core.Audio.SampleProviders.BalancePans;
using KeyAsio.Core.OsuAudio.Hitsounds.Playback;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Milky.OsuPlayer.Media.Audio;

internal sealed class OsuPlaybackEventDispatcher : IDisposable
{
    private readonly OsuEffectPlaybackBus _playbackBus;
    private readonly ILogger _logger;

    public OsuPlaybackEventDispatcher(OsuEffectPlaybackBus playbackBus, ILogger? logger = null)
    {
        _playbackBus = playbackBus;
        _logger = logger ?? NullLogger<OsuPlaybackEventDispatcher>.Instance;
    }

    public float HitsoundVolume
    {
        set => _playbackBus.HitsoundVolume = value;
    }

    public float SampleVolume
    {
        set => _playbackBus.BackgroundVolume = value;
    }

    public float BalanceFactor { get; set; } = 0.35f;
    public BalanceMode BalanceMode { get; set; } = BalanceMode.ConstantPower;

    public void Dispatch(PlaybackEvent playbackEvent, CachedAudio? cachedAudio)
    {
        switch (playbackEvent)
        {
            case SampleEvent sampleEvent:
                PlaySample(sampleEvent, cachedAudio);
                break;
            case ControlEvent controlEvent:
                PlayControl(controlEvent, cachedAudio);
                break;
        }
    }

    public void ClearLoops()
    {
        _playbackBus.StopAllBackgroundLoops();
    }

    public void Dispose()
    {
        _playbackBus.Dispose();
    }

    private void PlaySample(SampleEvent sampleEvent, CachedAudio? cachedAudio)
    {
        if (cachedAudio == null)
        {
            _logger?.LogWarning("Skip osu sample because cached audio is missing: {Filename}", sampleEvent.Filename);
            return;
        }

        // Storyboard and background samples are the "Sample/track" volume group.
        var track = sampleEvent.Layer == SampleLayer.Sampling
            ? OsuEffectTrack.Background
            : OsuEffectTrack.Hitsound;

        var volume = sampleEvent.Volume;
        if (sampleEvent.Layer == SampleLayer.Effects)
        {
            volume *= 1.25f;
        }

        try
        {
            _playbackBus.PlayOneShot(track, cachedAudio, volume, sampleEvent.Balance, BalanceMode, BalanceFactor);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error while playing osu sample: {Filename}", sampleEvent.Filename);
        }
    }

    private void PlayControl(ControlEvent controlEvent, CachedAudio? cachedAudio)
    {
        switch (controlEvent.ControlEventType)
        {
            case ControlEventType.LoopStart:
                if (cachedAudio == null)
                {
                    _logger?.LogWarning("Skip osu loop because cached audio is missing: {Filename}",
                        controlEvent.Filename);
                    return;
                }

                if (_playbackBus.HasBackgroundLoop((int)controlEvent.LoopChannel))
                {
                    _playbackBus.StopAllBackgroundLoops();
                }

                _playbackBus.StartBackgroundLoop((int)controlEvent.LoopChannel,
                    cachedAudio,
                    controlEvent.Volume,
                    controlEvent.Balance,
                    BalanceMode,
                    BalanceFactor);
                break;
            case ControlEventType.LoopStop:
                _playbackBus.StopBackgroundLoop((int)controlEvent.LoopChannel);
                break;
            case ControlEventType.Volume:
                _playbackBus.ChangeAllBackgroundLoopVolumes(controlEvent.Volume);
                break;
            case ControlEventType.Balance:
                _playbackBus.ChangeAllBackgroundLoopBalances(controlEvent.Balance, BalanceFactor);
                break;
        }
    }
}
