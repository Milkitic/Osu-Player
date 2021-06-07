using System;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio.Wave
{
    public class TimingSampleProvider : ISampleProvider
    {
        private ISampleProvider _sourceProvider;
        public TimeSpan CurrentTime { get; private set; } = TimeSpan.Zero;

        public event Action<TimeSpan, TimeSpan> Updated;

        public TimingSampleProvider(ISampleProvider sourceProvider)
        {
            _sourceProvider = sourceProvider;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _sourceProvider.Read(buffer, offset, count);
            var oldTime = CurrentTime;
            CurrentTime += SamplesToTimeSpan(samplesRead);
            if (oldTime != CurrentTime)
                Updated?.Invoke(oldTime, CurrentTime);
            return samplesRead;
        }

        public WaveFormat WaveFormat => _sourceProvider.WaveFormat;

        private int TimeSpanToSamples(TimeSpan time)
        {
            var samples = (int)(time.TotalSeconds * WaveFormat.SampleRate) * WaveFormat.Channels;
            return samples;
        }

        private TimeSpan SamplesToTimeSpan(int samples)
        {
            return TimeSpan.FromSeconds((samples / WaveFormat.Channels) / (double)WaveFormat.SampleRate);
        }
    }
}