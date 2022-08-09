using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Milki.OsuPlayer.Audio.New;

public class TrackPlayer : IAsyncDisposable
{
    private readonly AudioEngine _engine;
    private readonly TimerSource _timerSource;
    private readonly HashSet<ISampleProvider?> _trackHashSet;

    private double _previous;
    private TimerStatus _previousStatus = TimerStatus.Stop;
    private float _previousRate;

    public TrackPlayer(AudioEngine engine)
    {
        _engine = engine;
        _timerSource = new TimerSource();
        Tracks = new List<Track>();
        _trackHashSet = new HashSet<ISampleProvider?>();
        _timerSource.TimeUpdated += TimerSource_TimeUpdated;
        _timerSource.StatusChanged += TimerSource_StatusChanged;
        _timerSource.RateChanged += TimerSource_RateChanged;
    }

    public PlayerStatus PlayerStatus { get; private set; }
    public List<Track> Tracks { get; }
    public double Duration { get; protected set; }

    public async void Play()
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

    public async void Pause()
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

    public async void Stop()
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

    public async void Restart()
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

    public async void Seek(TimeSpan time)
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
        Play();
        PlayerStatus = PlayerStatus.Playing;
    }

    public virtual async ValueTask InitializeAsync()
    {
        foreach (var track in Tracks)
        {
            await track.InitializeAsync();
        }

        Duration = Tracks.Max(k => k.Duration);
        foreach (var track in Tracks)
        {
            if (track is SoundSeekingTrack)
            {
                if (_timerSource.IsRunning && track.RootSampleProvider != null)
                {
                    AddToMusicMixer(track.RootSampleProvider);
                }
            }
            else
            {
                if (track.RootSampleProvider != null)
                {
                    _engine.EffectMixer.AddMixerInput(track.RootSampleProvider);
                }
            }
        }

        PlayerStatus = PlayerStatus.Ready;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var track in Tracks)
        {
            await track.DisposeAsync();
        }
    }

    public virtual void OnUpdated(double previous, double current) { }

    public virtual void OnStatusChanged(TimerStatus previousStatus, TimerStatus currentStatus)
    {
        if (previousStatus is TimerStatus.Start or TimerStatus.Restart &&
            currentStatus is TimerStatus.Stop or TimerStatus.Reset)
        {
            foreach (var track in Tracks.Where(k => k is SoundSeekingTrack))
            {
                RemoveFromMusicMixer(track.RootSampleProvider);
            }
        }
        else if (previousStatus is TimerStatus.Stop or TimerStatus.Reset &&
                 currentStatus is TimerStatus.Start or TimerStatus.Restart)
        {
            foreach (var track in Tracks.Where(k => k is SoundSeekingTrack))
            {
                AddToMusicMixer(track.RootSampleProvider);
            }
        }
    }

    public virtual void OnRateChanged(float previousRate, float currentRate) { }


    protected virtual ValueTask PlayCoreAsync()
    {
        if (!_timerSource.IsRunning)
        {
            _timerSource.Start();
        }

        return ValueTask.CompletedTask;
    }

    protected virtual void PauseCore()
    {
        _timerSource.Stop();
    }

    protected virtual void StopCore()
    {
        _timerSource.Reset();
    }

    protected virtual ValueTask SeekCore(TimeSpan time)
    {
        _timerSource.SkipTo(time.TotalMilliseconds);
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask StopSeekingAsync()
    {
        return ValueTask.CompletedTask;
    }

    private void AddToMusicMixer(ISampleProvider? sampleProvider)
    {
        if (sampleProvider != null) return;
        if (_trackHashSet.Contains(sampleProvider)) return;
        _trackHashSet.Add(sampleProvider);
        _engine.MusicMixer.AddMixerInput(sampleProvider);
    }

    private void RemoveFromMusicMixer(ISampleProvider? sampleProvider)
    {
        if (sampleProvider != null) return;
        _trackHashSet.Remove(sampleProvider);
        _engine.MusicMixer.RemoveMixerInput(sampleProvider);
    }

    private void TimerSource_TimeUpdated(double current)
    {
        OnUpdated(_previous, current);
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

public enum PlayerStatus
{
    Uninitialized, Ready, Playing, Paused, Seeking
}