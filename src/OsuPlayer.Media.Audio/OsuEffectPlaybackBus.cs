using System;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using KeyAsio.Core.Audio.SampleProviders;
using KeyAsio.Core.Audio.SampleProviders.BalancePans;
using KeyAsio.Core.Audio.Utils;

namespace Milky.OsuPlayer.Media.Audio;

internal enum OsuEffectTrack
{
    Background,
    Hitsound
}

internal sealed class OsuEffectPlaybackBus : IDisposable
{
    private readonly IMixingSampleProvider _parentMixer;
    private readonly QueueMixingSampleProvider _backgroundMixer;
    private readonly QueueMixingSampleProvider _hitsoundMixer;
    private readonly EnhancedVolumeSampleProvider _backgroundVolumeProvider;
    private readonly EnhancedVolumeSampleProvider _hitsoundVolumeProvider;
    private readonly LoopProviderManager _backgroundLoopProviderManager = new();

    public OsuEffectPlaybackBus(IMixingSampleProvider parentMixer)
    {
        _parentMixer = parentMixer;

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

        _parentMixer.AddMixerInput(_backgroundVolumeProvider);
        _parentMixer.AddMixerInput(_hitsoundVolumeProvider);
    }

    public float HitsoundVolume
    {
        set => _hitsoundVolumeProvider.Volume = value;
    }

    public float BackgroundVolume
    {
        set => _backgroundVolumeProvider.Volume = value;
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
        _parentMixer.RemoveMixerInput(_backgroundVolumeProvider);
        _parentMixer.RemoveMixerInput(_hitsoundVolumeProvider);
        _backgroundMixer.Dispose();
        _hitsoundMixer.Dispose();
    }

    private IMixingSampleProvider GetMixer(OsuEffectTrack track)
    {
        return track == OsuEffectTrack.Background ? _backgroundMixer : _hitsoundMixer;
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
