﻿using Milki.Extensions.MixPlayer.NAudioExtensions;
using NAudio.Wave;

namespace Milki.OsuPlayer.Audio.Mixing;

public class TrackPlayer : IAsyncDisposable
{
    public event Action<TrackPlayer, PlayerStatus, PlayerStatus>? PlayerStatusChanged;
    public event Action<TrackPlayer, double, double>? PositionChanged;

    protected readonly TimerSource TimerSource;
    protected readonly AudioPlaybackEngine Engine;

    private readonly HashSet<ISampleProvider?> _trackHashSet;

    private PlayerStatus _playerStatus;
    private TimerStatus _previousStatus = TimerStatus.Stop;
    private float _previousRate;
    private double _previousPosition;
    private double _position;

    public TrackPlayer(AudioPlaybackEngine engine, TimerSource? timerSource = null)
    {
        Engine = engine;
        TimerSource = timerSource ?? new TimerSource(3);
        Tracks = new List<Track>();
        _trackHashSet = new HashSet<ISampleProvider?>();
        TimerSource.TimeUpdated += TimerSource_TimeUpdated;
        TimerSource.StatusChanged += TimerSource_StatusChanged;
        TimerSource.RateChanged += TimerSource_RateChanged;
    }

    public double PreInsertDuration
    {
        get => -TimerSource.StartTime;
        set => TimerSource.StartTime = -value;
    }

    public double PostInsertDuration { get; set; }

    public PlayerStatus PlayerStatus
    {
        get => _playerStatus;
        protected set
        {
            if (_playerStatus == value) return;
            var oldValue = _playerStatus;
            _playerStatus = value;
            PlayerStatusChanged?.Invoke(this, oldValue, value);
        }
    }

    public double Position
    {
        get => _position;
        protected set
        {
            if (Equals(_position, value)) return;
            var oldValue = _position;
            _position = value;
            PositionChanged?.Invoke(this, oldValue, value);
        }
    }

    public List<Track> Tracks { get; }

    public double Duration { get; protected set; }

    public async ValueTask Play()
    {
        if (PlayerStatus is PlayerStatus.Uninitialized)
        {
            throw new InvalidOperationException("Player is not initialized.");
        }

        if (PlayerStatus is PlayerStatus.Seeking)
        {
            await StopSeekingAsync();
        }

        PlayerStatus = PlayerStatus.Seeking;
        await PlayCoreAsync();
        PlayerStatus = PlayerStatus.Playing;
    }

    public async ValueTask Pause()
    {
        if (PlayerStatus is PlayerStatus.Uninitialized)
        {
            throw new InvalidOperationException("Player is not initialized.");
        }

        if (PlayerStatus is PlayerStatus.Seeking)
        {
            await StopSeekingAsync();
        }

        PlayerStatus = PlayerStatus.Seeking;
        PauseCore();
        PlayerStatus = PlayerStatus.Paused;
    }

    public void TogglePlay()
    {
        if (PlayerStatus is PlayerStatus.Ready or PlayerStatus.Paused)
        {
            Play();
        }
        else if (PlayerStatus is PlayerStatus.Playing)
        {
            Pause();
        }
    }

    public async ValueTask Stop()
    {
        if (PlayerStatus is PlayerStatus.Uninitialized)
        {
            throw new InvalidOperationException("Player is not initialized.");
        }

        if (PlayerStatus is PlayerStatus.Seeking)
        {
            await StopSeekingAsync();
        }

        PlayerStatus = PlayerStatus.Seeking;
        StopCore();
        PlayerStatus = PlayerStatus.Ready;
    }

    public async ValueTask Restart()
    {
        if (PlayerStatus is PlayerStatus.Uninitialized)
        {
            throw new InvalidOperationException("Player is not initialized.");
        }

        if (PlayerStatus is PlayerStatus.Seeking)
        {
            await StopSeekingAsync();
        }

        PlayerStatus = PlayerStatus.Seeking;
        StopCore();
        await PlayCoreAsync();
        PlayerStatus = PlayerStatus.Playing;
    }

    public async ValueTask Seek(TimeSpan time)
    {
        if (PlayerStatus is PlayerStatus.Uninitialized)
        {
            throw new InvalidOperationException("Player is not initialized.");
        }

        if (PlayerStatus is PlayerStatus.Seeking)
        {
            await StopSeekingAsync();
        }

        PlayerStatus = PlayerStatus.Seeking;
        StopCore();
        await SeekCore(time);
        await PlayCoreAsync(); // todo: don't force to play
        PlayerStatus = PlayerStatus.Playing;
    }

    public void SetRate(float rate, bool keepTune)
    {
        TimerSource.Rate = rate;

        foreach (var track in Tracks)
        {
            track.KeepTune = keepTune;
        }
    }

    public virtual async ValueTask InitializeAsync()
    {
        foreach (var track in Tracks)
        {
            await track.InitializeAsync();
        }

        Duration = Tracks.Max(k => k.Duration) + PostInsertDuration;
        foreach (var track in Tracks)
        {
            if (track is SoundSeekingTrack)
            {
                if (TimerSource.IsRunning && track.RootSampleProvider != null)
                {
                    AddToMixer(track.RootSampleProvider);
                }
            }
            else
            {
                if (track.RootSampleProvider != null)
                {
                    Engine.AddMixerInput(track.RootSampleProvider);
                }
            }
        }

        PlayerStatus = PlayerStatus.Ready;
    }

    public virtual async ValueTask DisposeAsync()
    {
        foreach (var track in Tracks)
        {
            await track.DisposeAsync();
            RemoveFromMixer(track.RootSampleProvider);
        }

        Tracks.Clear();
    }

    public virtual void OnUpdated(double previous, double current) { }

    public virtual void OnStatusChanged(TimerStatus previousStatus, TimerStatus currentStatus)
    {
        if (previousStatus is TimerStatus.Start or TimerStatus.Restart &&
            currentStatus is TimerStatus.Stop or TimerStatus.Reset)
        {
            foreach (var track in Tracks.Where(k => k is SoundSeekingTrack))
            {
                RemoveFromMixer(track.RootSampleProvider);
            }
        }
        else if (previousStatus is TimerStatus.Stop or TimerStatus.Reset &&
                 currentStatus is TimerStatus.Start or TimerStatus.Restart)
        {
            foreach (var track in Tracks.Where(k => k is SoundSeekingTrack))
            {
                AddToMixer(track.RootSampleProvider);
            }
        }
    }

    public virtual void OnRateChanged(float previousRate, float currentRate) { }

    protected virtual ValueTask PlayCoreAsync()
    {
        if (!TimerSource.IsRunning)
        {
            TimerSource.Start();
        }

        return ValueTask.CompletedTask;
    }

    protected virtual void PauseCore()
    {
        TimerSource.Stop();
    }

    protected virtual void StopCore()
    {
        TimerSource.Reset();
    }

    protected virtual ValueTask SeekCore(TimeSpan time)
    {
        TimerSource.SkipTo(time.TotalMilliseconds);
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask StopSeekingAsync()
    {
        return ValueTask.CompletedTask;
    }

    private void AddToMixer(ISampleProvider? sampleProvider)
    {
        if (sampleProvider == null) return;
        if (_trackHashSet.Contains(sampleProvider)) return;
        _trackHashSet.Add(sampleProvider);
        Engine.AddMixerInput(sampleProvider);
    }

    private void RemoveFromMixer(ISampleProvider? sampleProvider)
    {
        if (sampleProvider == null) return;
        _trackHashSet.Remove(sampleProvider);
        Engine.RemoveMixerInput(sampleProvider);
    }

    private void TimerSource_TimeUpdated(double current)
    {
        OnUpdated(_previousPosition, current);
        _previousPosition = current;
        if (current >= Duration)
        {
            Position = Duration;
            if (TimerSource.IsRunning && PlayerStatus == PlayerStatus.Playing)
            {
                StopCore();
                PlayerStatus = PlayerStatus.Ready;
            }
        }
        else
        {
            Position = current;
        }
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

public enum PlayerStatus
{
    Uninitialized, Ready, Playing, Paused, Seeking
}