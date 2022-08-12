using System;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Milki.OsuPlayer.Audio.Mixing;

public abstract class Track : ITrack
{
    protected readonly TimerSource TimerSource;

    private double _duration;
    private double _previous;
    private TimerStatus _previousStatus;
    private float _previousRate;

    public Track(TimerSource timerSource)
    {
        TimerSource = timerSource;
        _previous = 0d;
        timerSource.TimeUpdated += TimerSource_TimeUpdated;
        timerSource.StatusChanged += TimerSource_StatusChanged;
        timerSource.RateChanged += TimerSource_RateChanged;
    }

    protected bool IsInitialized { get; private set; }
    public ISampleProvider? RootSampleProvider { get; protected set; }

    public double Offset { get; set; }

    public double Duration
    {
        get => IsInitialized ? _duration : throw new InvalidOperationException("Track is not initialized.");
        protected set => _duration = value;
    }

    public async ValueTask InitializeAsync()
    {
        await InitializeCoreAsync();
        IsInitialized = true;
    }

    public abstract ValueTask DisposeAsync();
    public virtual void OnUpdated(double previous, double current) { }
    public virtual void OnStatusChanged(TimerStatus previousStatus, TimerStatus currentStatus) { }
    public virtual void OnRateChanged(float previousRate, float currentRate) { }

    protected abstract ValueTask InitializeCoreAsync();

    private void TimerSource_TimeUpdated(double current)
    {
        var offset = Offset;
        OnUpdated(_previous - offset, current - offset);
        _previous = current;
    }

    private void TimerSource_StatusChanged(TimerStatus currentStatus)
    {
        OnStatusChanged(_previousStatus, currentStatus);
        _previousStatus = currentStatus;
    }

    private void TimerSource_RateChanged(float currentRate)
    {
        OnRateChanged(_previousRate, currentRate);
        _previousRate = currentRate;
    }
}