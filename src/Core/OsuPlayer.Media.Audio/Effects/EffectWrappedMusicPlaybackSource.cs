using System;
using System.Threading;
using System.Threading.Tasks;
using KeyAsio.Core.Audio;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Media.Audio.Effects;

/// <summary>
/// Decorates an inner <see cref="IMusicPlaybackSource"/> so that
/// whatever the transport registers with the music mixer is our
/// <see cref="EffectChainSampleProvider"/> rather than the raw
/// provider. The wrapper re-emits <see cref="OutputChanged"/> when the
/// inner source swaps its output (e.g. a playback rate change), which
/// keeps KeyAsio's <c>StandaloneMusicTransport</c> in sync without
/// requiring any change on its side.
/// </summary>
internal sealed class EffectWrappedMusicPlaybackSource : IMusicPlaybackSource
{
    private readonly IMusicPlaybackSource _inner;
    private readonly EffectParameterSet _parameters;
    private EffectChainSampleProvider _effectOutput;

    public EffectWrappedMusicPlaybackSource(IMusicPlaybackSource inner, EffectParameterSet parameters)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        _effectOutput = new EffectChainSampleProvider(_inner.Output, _parameters);
        _inner.OutputChanged += OnInnerOutputChanged;
    }

    public event Action<ISampleProvider, ISampleProvider>? OutputChanged;

    public WaveFormat WaveFormat => _inner.WaveFormat;
    public TimeSpan Duration => _inner.Duration;
    public TimeSpan Position => _inner.Position;
    public PlaybackRateState RateState => _inner.RateState;
    public bool IsRunning => _inner.IsRunning;
    public ISampleProvider Output => _effectOutput;
    public bool SupportsPlaybackRateChange => _inner.SupportsPlaybackRateChange;

    public Task PlayAsync(CancellationToken cancellationToken = default)
        => _inner.PlayAsync(cancellationToken);

    public Task PauseAsync(CancellationToken cancellationToken = default)
        => _inner.PauseAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default)
        => _inner.StopAsync(cancellationToken);

    public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
        => _inner.SeekAsync(position, cancellationToken);

    public Task SetPlaybackRateAsync(PlaybackRateState rateState, CancellationToken cancellationToken = default)
        => _inner.SetPlaybackRateAsync(rateState, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        _inner.OutputChanged -= OnInnerOutputChanged;
        await _inner.DisposeAsync().ConfigureAwait(false);
    }

    private void OnInnerOutputChanged(ISampleProvider oldOutput, ISampleProvider newOutput)
    {
        // The inner source swapped its output (e.g. a rate processor
        // got inserted). Build a fresh EffectChainSampleProvider
        // around the new output and re-apply the active effect so the
        // transport registers a wrapper that reads from the right
        // upstream.
        var kind = _effectOutput.ActiveKind;
        var intensity = _effectOutput.ActiveIntensity;

        var newWrapper = new EffectChainSampleProvider(newOutput, _parameters);
        if (kind != DirectXEffectKind.None)
        {
            newWrapper.SetEffect(kind, intensity);
        }
        _effectOutput = newWrapper;
        OutputChanged?.Invoke(oldOutput, newWrapper);
    }
}
