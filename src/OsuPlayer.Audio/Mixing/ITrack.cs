using System;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Milki.OsuPlayer.Audio.Mixing;

public interface ITrack : IAsyncDisposable
{
    double Duration { get; }
    ISampleProvider? RootSampleProvider { get; }

    ValueTask InitializeAsync();
    void OnUpdated(double previous, double current);
    void OnStatusChanged(TimerStatus previousStatus, TimerStatus currentStatus);
    void OnRateChanged(float previousRate, float currentRate);
}