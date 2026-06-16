using System;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using KeyAsio.Core.Audio.SampleProviders;
using KeyAsio.Core.Audio.SampleProviders.BalancePans;
using KeyAsio.Core.Audio.Utils;
using KeyAsio.Core.OsuAudio.Hitsounds.Playback;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Media.Audio.Effects;

namespace OsuPlayer.Media.Audio;

internal enum OsuEffectTrack
{
    Background,
    Hitsound
}

internal sealed class OsuEffectPlaybackBus : IDisposable
{
    private readonly IMixingSampleProvider _parentMixer;
    private readonly ILogger _logger;
    private readonly EffectParameterSet _parameters;
    private readonly QueueMixingSampleProvider _backgroundMixer;
    private readonly QueueMixingSampleProvider _hitsoundMixer;
    private readonly EnhancedVolumeSampleProvider _backgroundVolumeProvider;
    private readonly EnhancedVolumeSampleProvider _hitsoundVolumeProvider;
    private readonly EffectChainSampleProvider _backgroundEffectChain;
    private readonly EffectChainSampleProvider _hitsoundEffectChain;
    private readonly LoopProviderManager _backgroundLoopProviderManager = new();

    public OsuEffectPlaybackBus(IMixingSampleProvider parentMixer, EffectParameterSet parameters, ILogger? logger = null)
    {
        _parentMixer = parentMixer;
        _parameters = parameters;
        _logger = logger ?? NullLogger<OsuEffectPlaybackBus>.Instance;

        _backgroundMixer = CreateChildMixer(parentMixer.WaveFormat);
        _hitsoundMixer = CreateChildMixer(parentMixer.WaveFormat);

        _backgroundVolumeProvider = new EnhancedVolumeSampleProvider(_backgroundMixer)
        {
            ExcludeFromPool = true
        };
        _hitsoundVolumeProvider = new EnhancedVolumeSampleProvider(_hitsoundMixer)
        {
            ExcludeFromPool = true
        };

        // Wrap the post-volume stage with an effect chain so the
        // mixer sees our wrapper; the chain itself is normally a
        // pass-through and only activates when settings are pushed
        // via ApplyEffectsSettings.
        _backgroundEffectChain = new EffectChainSampleProvider(_backgroundVolumeProvider, _parameters);
        _hitsoundEffectChain = new EffectChainSampleProvider(_hitsoundVolumeProvider, _parameters);

        _parentMixer.AddMixerInput(_backgroundEffectChain);
        _parentMixer.AddMixerInput(_hitsoundEffectChain);
    }

    public float HitsoundVolume
    {
        set => _hitsoundVolumeProvider.Volume = value;
    }

    public float BackgroundVolume
    {
        set => _backgroundVolumeProvider.Volume = value;
    }

    public float SampleVolume
    {
        set => BackgroundVolume = value;
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
        StopAllBackgroundLoops();
    }

    public void PlayOneShot(OsuEffectTrack track, CachedAudio cachedAudio, float volume, float balance,
        BalanceMode balanceMode, float balanceFactor)
    {
        if (cachedAudio.Length == 0 || cachedAudio.IsDisposingOrDisposed || volume <= 0)
        {
            return;
        }

        var cachedAudioProvider = RecyclableSampleProviderFactory.RentCacheProvider(cachedAudio);
        var volumeProvider = RecyclableSampleProviderFactory.RentVolumeProvider(cachedAudioProvider, volume);
        var balanceProvider = RecyclableSampleProviderFactory.RentBalanceProvider(
            volumeProvider,
            balance * balanceFactor,
            balanceMode,
            AntiClipStrategy.None);

        GetMixer(track).AddMixerInput(balanceProvider);
    }

    public bool HasBackgroundLoop(int channel)
    {
        return _backgroundLoopProviderManager.ShouldRemoveAll(channel);
    }

    public void StartBackgroundLoop(int channel, CachedAudio cachedAudio, float volume, float balance,
        BalanceMode balanceMode, float balanceFactor)
    {
        _backgroundLoopProviderManager.Create(
            channel,
            cachedAudio,
            _backgroundMixer,
            volume,
            balance,
            balanceMode,
            volumeFactor: 1,
            balanceFactor: balanceFactor);
    }

    public void StopBackgroundLoop(int channel)
    {
        _backgroundLoopProviderManager.Remove(channel, _backgroundMixer);
    }

    public void StopAllBackgroundLoops()
    {
        _backgroundLoopProviderManager.RemoveAll(_backgroundMixer);
    }

    public void ChangeAllBackgroundLoopVolumes(float volume)
    {
        _backgroundLoopProviderManager.ChangeAllVolumes(volume, volumeFactor: 1);
    }

    public void ChangeAllBackgroundLoopBalances(float balance, float balanceFactor)
    {
        _backgroundLoopProviderManager.ChangeAllBalances(balance, balanceFactor);
    }

    public void Dispose()
    {
        StopAllBackgroundLoops();
        _parentMixer.RemoveMixerInput(_backgroundEffectChain);
        _parentMixer.RemoveMixerInput(_hitsoundEffectChain);
        _backgroundMixer.Dispose();
        _hitsoundMixer.Dispose();
    }

    /// <summary>
    /// Pushes the current effect configuration to the per-bus chains.
    /// Either bus can be disabled individually (the chain returns to
    /// pass-through) so a single settings object can drive all three
    /// effect-capable buses (hitsound, background, music) without each
    /// bus having to know the others exist.
    /// </summary>
    public void ApplyEffectsSettings(DirectXEffectSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _hitsoundEffectChain.SetEffect(
            settings.ApplyToHitsound ? settings.Kind : DirectXEffectKind.None,
            settings.Intensity);
        _backgroundEffectChain.SetEffect(
            settings.ApplyToBackground ? settings.Kind : DirectXEffectKind.None,
            settings.Intensity);
    }

    private IMixingSampleProvider GetMixer(OsuEffectTrack track)
    {
        return track == OsuEffectTrack.Background ? _backgroundMixer : _hitsoundMixer;
    }

    private void PlaySample(SampleEvent sampleEvent, CachedAudio? cachedAudio)
    {
        if (cachedAudio == null)
        {
            _logger.LogWarning("Skip osu sample because cached audio is missing: {Filename}", sampleEvent.Filename);
            return;
        }

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
            PlayOneShot(track, cachedAudio, volume, sampleEvent.Balance, BalanceMode, BalanceFactor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while playing osu sample: {Filename}", sampleEvent.Filename);
        }
    }

    private void PlayControl(ControlEvent controlEvent, CachedAudio? cachedAudio)
    {
        switch (controlEvent.ControlEventType)
        {
            case ControlEventType.LoopStart:
                if (cachedAudio == null)
                {
                    _logger.LogWarning("Skip osu loop because cached audio is missing: {Filename}",
                        controlEvent.Filename);
                    return;
                }

                if (HasBackgroundLoop((int)controlEvent.LoopChannel))
                {
                    StopAllBackgroundLoops();
                }

                StartBackgroundLoop((int)controlEvent.LoopChannel,
                    cachedAudio,
                    controlEvent.Volume,
                    controlEvent.Balance,
                    BalanceMode,
                    BalanceFactor);
                break;
            case ControlEventType.LoopStop:
                StopBackgroundLoop((int)controlEvent.LoopChannel);
                break;
            case ControlEventType.Volume:
                ChangeAllBackgroundLoopVolumes(controlEvent.Volume);
                break;
            case ControlEventType.Balance:
                ChangeAllBackgroundLoopBalances(controlEvent.Balance, BalanceFactor);
                break;
        }
    }

    private static QueueMixingSampleProvider CreateChildMixer(NAudio.Wave.WaveFormat waveFormat)
    {
        return new QueueMixingSampleProvider(waveFormat)
        {
            ReadFully = true,
            WantsKeep = true
        };
    }
}
