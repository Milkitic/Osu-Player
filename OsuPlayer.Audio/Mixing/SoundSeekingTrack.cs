using System;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

namespace Milki.OsuPlayer.Audio.Mixing;

public class SoundSeekingTrack : Track
{
    private readonly CachedSound _cachedSound;
    private readonly VariableSpeedOptions _sharedVariableSpeedOptions;
    private SeekableCachedSoundSampleProvider? _cachedSoundSampleProvider;
    private VariableSpeedSampleProvider? _variableSpeedSampleProvider;
    private bool _keepTune;

    public SoundSeekingTrack(CachedSound cachedSound, TimerSource timerSource) : base(timerSource)
    {
        _cachedSound = cachedSound;
        _sharedVariableSpeedOptions = new VariableSpeedOptions(true, true);
    }

    public bool KeepTune
    {
        get => _keepTune;
        set
        {
            if (value.Equals(_keepTune)) return;
            _keepTune = value;
            var sampleProvider = _variableSpeedSampleProvider;
            if (sampleProvider == null) return;

            _sharedVariableSpeedOptions.KeepTune = value;
            sampleProvider.SetSoundTouchProfile(_sharedVariableSpeedOptions);
        }
    }

    public override void OnUpdated(double previous, double current)
    {
        var sampleProvider = _cachedSoundSampleProvider;
        if (sampleProvider == null) return;
        var diffTolerance = GetDifferenceTolerance();

        var currentPlayTime = sampleProvider.PlayTime.TotalMilliseconds;
        var diffMilliseconds = Math.Abs(currentPlayTime - current);
        if (diffMilliseconds > diffTolerance)
        {
            //Logger.Debug($"Music offset too large {diffMilliseconds:N2}ms for {diffTolerance:N0}ms, will force to seek.");
            sampleProvider.PlayTime = TimeSpan.FromMilliseconds(current);
        }
    }

    public override void OnRateChanged(float previousRate, float currentRate)
    {
        var sampleProvider = _variableSpeedSampleProvider;
        if (sampleProvider == null) return;
        sampleProvider.PlaybackRate = currentRate;
    }

    public override ValueTask DisposeAsync()
    {
        _variableSpeedSampleProvider?.Dispose();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask InitializeCoreAsync()
    {
        _cachedSoundSampleProvider = new SeekableCachedSoundSampleProvider(_cachedSound);
        _variableSpeedSampleProvider = new VariableSpeedSampleProvider(_cachedSoundSampleProvider,
            readDurationMilliseconds: 10, _sharedVariableSpeedOptions);
        _variableSpeedSampleProvider.PlaybackRate = TimerSource.Rate;
        Duration = _cachedSound.Duration.TotalMilliseconds;
        RootSampleProvider = _variableSpeedSampleProvider;
        return ValueTask.CompletedTask;
    }

    protected virtual double GetDifferenceTolerance() => 10;
}