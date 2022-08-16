using Anotar.NLog;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.OsuPlayer.Audio.Mixing;

public class SoundSeekingTrack : Track
{
    private readonly WaveFormat _waveFormat;
    private readonly VariableSpeedOptions _sharedVariableSpeedOptions;
    private readonly EnhancedVolumeSampleProvider _volumeSampleProvider;

    private SeekableCachedSoundSampleProvider? _cachedSoundSampleProvider;
    private SmartWaveReader? _smartWaveReader;
    private VariableSpeedSampleProvider? _variableSpeedSampleProvider;

    private string? _path;
    private bool _keepTune;
    private bool _readFully = true;
    private double _previousCurrentPlayTime;
    private ISampleProvider? _normalSampleProvider;

    public SoundSeekingTrack(TimerSource timerSource, WaveFormat waveFormat) : base(timerSource)
    {
        _waveFormat = waveFormat;
        _sharedVariableSpeedOptions = new VariableSpeedOptions(true, true);
        _volumeSampleProvider = new EnhancedVolumeSampleProvider(null);
    }

    public string? Path
    {
        get => _path;
        set => _path = IsInitialized
            ? throw new InvalidOperationException("Could not change path after track initialized.")
            : value;
    }

    public bool ReadFully
    {
        get => _readFully;
        set => _readFully = IsInitialized
            ? throw new InvalidOperationException("Could not change option after track initialized.")
            : value;
    }

    public override bool KeepTune
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
        if (Math.Abs(_previousCurrentPlayTime - currentPlayTime) <= 0.0001) return;
        var diffMilliseconds = Math.Abs(currentPlayTime - current);
        if (diffMilliseconds > diffTolerance)
        {
            LogTo.Debug($"Music offset too large {diffMilliseconds:N2}ms for {diffTolerance:N0}ms, will force to seek.");
            sampleProvider.PlayTime = TimeSpan.FromMilliseconds(current);
        }

        _previousCurrentPlayTime = currentPlayTime;
    }

    public override void OnRateChanged(float previousRate, float currentRate)
    {
        var sampleProvider = _variableSpeedSampleProvider;
        if (sampleProvider == null) return;
        ChangeSourceByTimer();
        sampleProvider.PlaybackRate = currentRate;
    }

    public override ValueTask DisposeAsync()
    {
        _variableSpeedSampleProvider?.Dispose();
        return ValueTask.CompletedTask;
    }

    protected override async ValueTask InitializeCoreAsync()
    {
        if (Path != null)
        {
            if (ReadFully)
            {
                var cachedSound = (await CachedSoundFactory.GetOrCreateCacheSound(_waveFormat, Path, checkFileExist: false,
                    useWdlResampler: true))!;
                _cachedSoundSampleProvider = new SeekableCachedSoundSampleProvider(cachedSound);
                _normalSampleProvider = _cachedSoundSampleProvider;
                _variableSpeedSampleProvider = new VariableSpeedSampleProvider(_cachedSoundSampleProvider,
                    readDurationMilliseconds: 10, _sharedVariableSpeedOptions);
                Duration = cachedSound.Duration.TotalMilliseconds;
            }
            else
            {
                _normalSampleProvider = _smartWaveReader = new SmartWaveReader(Path);
                if (_smartWaveReader.WaveFormat.Channels == 1)
                    _normalSampleProvider = new MonoToStereoSampleProvider(_normalSampleProvider);
                if (_smartWaveReader.WaveFormat.SampleRate != _waveFormat.SampleRate)
                    _normalSampleProvider = new WdlResamplingSampleProvider(_normalSampleProvider, _waveFormat.SampleRate);
                _variableSpeedSampleProvider = new VariableSpeedSampleProvider(_normalSampleProvider,
                    readDurationMilliseconds: 10, _sharedVariableSpeedOptions);
                Duration = _smartWaveReader.TotalTime.TotalMilliseconds;
            }
        }
        else
        {
            throw new Exception("Path not specified.");
        }

        ChangeSourceByTimer();

        RootSampleProvider = _volumeSampleProvider;
        _variableSpeedSampleProvider.PlaybackRate = TimerSource.Rate;
    }

    private void ChangeSourceByTimer()
    {
        _volumeSampleProvider.Source = Math.Abs(TimerSource.Rate - 1f) < 0.005
            ? _normalSampleProvider
            : _variableSpeedSampleProvider;
    }

    protected virtual double GetDifferenceTolerance() => 8;
}