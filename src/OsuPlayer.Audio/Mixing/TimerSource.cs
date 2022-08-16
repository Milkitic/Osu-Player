using System.Diagnostics;

namespace Milki.OsuPlayer.Audio.Mixing;

public class TimerSource
{
    public event Action<TimerStatus>? StatusChanged;
    public event Action<double>? TimeUpdated;
    public event Action<float>? RateChanged;

    private readonly Stopwatch _stopwatch;
    private double _offset;
    private CancellationTokenSource? _cts;
    private float _rate = 1;

    public TimerSource(double notifyIntervalMillisecond = 1)
    {
        _stopwatch = new Stopwatch();
        NotifyIntervalMillisecond = notifyIntervalMillisecond;
    }

    public long ElapsedMilliseconds =>
        (long)(_stopwatch.Elapsed.TotalMilliseconds * Rate + _offset);

    public TimeSpan Elapsed =>
        TimeSpan.FromMilliseconds(_stopwatch.Elapsed.TotalMilliseconds * Rate + _offset);

    public bool IsRunning => _stopwatch.IsRunning;

    public float Rate
    {
        get => _rate;
        set
        {
            if (value.Equals(_rate)) return;
            _rate = value;
            RateChanged?.Invoke(value);
        }
    }

    public double NotifyIntervalMillisecond { get; set; }

    public void Start()
    {
        var created = _stopwatch.IsRunning;
        _stopwatch.Start();
        StatusChanged?.Invoke(TimerStatus.Start);
        TimeUpdated?.Invoke(ElapsedMilliseconds);
        if (!created)
        {
            CreateTask();
        }
    }

    public void Stop()
    {
        _stopwatch.Stop();
        StatusChanged?.Invoke(TimerStatus.Stop);
        if (_cts != null)
        {
            _cts.Cancel();
            _cts = null;
        }
    }

    public void Restart()
    {
        _offset = 0;
        _stopwatch.Stop();
        if (_cts != null)
        {
            _cts.Cancel();
            _cts = null;
        }

        _stopwatch.Restart();
        StatusChanged?.Invoke(TimerStatus.Restart);
        TimeUpdated?.Invoke(ElapsedMilliseconds);

        CreateTask();
    }

    public void Reset()
    {
        _offset = 0;
        _stopwatch.Reset();
        if (_cts != null)
        {
            _cts.Cancel();
            _cts = null;
        }

        StatusChanged?.Invoke(TimerStatus.Reset);
    }

    public void SkipTo(double offset)
    {
        _offset = offset;
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Restart();
        }
        else
        {
            _stopwatch.Reset();
        }

        StatusChanged?.Invoke(TimerStatus.Skip);
    }

    private void TimerLoop(CancellationTokenSource cts)
    {
        double loopLastTime = _stopwatch.Elapsed.TotalMilliseconds * Rate + _offset;
        TimeUpdated?.Invoke(loopLastTime);
        var spinWait = new SpinWait();
        while (!cts.IsCancellationRequested)
        {
            if (_stopwatch.IsRunning)
            {
                var elapsedMilliseconds = _stopwatch.Elapsed.TotalMilliseconds * Rate + _offset;
                if (elapsedMilliseconds - loopLastTime > NotifyIntervalMillisecond)
                {
                    TimeUpdated?.Invoke(elapsedMilliseconds);
                    loopLastTime = elapsedMilliseconds;
                }
            }
            else
            {
                break;
            }

            spinWait.SpinOnce();
        }

        cts.Dispose();
    }

    private void CreateTask()
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => TimerLoop(_cts));
    }
}