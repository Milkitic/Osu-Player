using System;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Media.Audio.Effects;

/// <summary>
/// Internal contract implemented by every DirectSound-style effect provider
/// in this project. The base class
/// <see cref="DirectXEffectProviderBase"/> implements the
/// <see cref="ISampleProvider"/> glue; concrete effects only fill in
/// <see cref="DirectXEffectProviderBase.Process"/> and react to intensity
/// changes.
/// </summary>
internal interface IDirectXEffectProvider : ISampleProvider
{
    /// <summary>
    /// Updates the effect in response to a new master intensity value in
    /// the range <c>[-1, +1]</c>. <c>-1</c> means "bypass / dry" and
    /// <c>+1</c> means "maximum effect strength".
    /// </summary>
    void SetIntensity(float intensity);

    /// <summary>
    /// Clears any stateful buffers (delay lines, LFOs, envelopes, filter
    /// histories) so the next read starts from silence. Called when the
    /// effect is being removed from the chain or replaced.
    /// </summary>
    void ResetState();
}
