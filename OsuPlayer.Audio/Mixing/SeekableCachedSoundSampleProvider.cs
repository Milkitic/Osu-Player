using System;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave;

namespace Milki.OsuPlayer.Audio.Mixing;

public class SeekableCachedSoundSampleProvider : ISampleProvider
{
    private readonly CachedSound _sourceSound;
    private readonly float[] _audioData;
    private readonly int _preSamples;
    private int _position;

    public SeekableCachedSoundSampleProvider(in CachedSound cachedSound, int leadInMilliseconds = 0)
    {
        _sourceSound = cachedSound;
        if (leadInMilliseconds != 0)
        {
            _preSamples = TimeSpanToSamples(TimeSpan.FromMilliseconds(leadInMilliseconds));
            _position = _preSamples;
        }

        _audioData = cachedSound.AudioData;
    }

    public WaveFormat WaveFormat => _sourceSound.WaveFormat;

    public TimeSpan PlayTime
    {
        get
        {
            lock (_sourceSound)
            {
                return SamplesToTimeSpan(_position - _preSamples);
            }
        }
        set
        {
            lock (_sourceSound)
            {
                _position = TimeSpanToSamples(value) + _preSamples;
            }
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        lock (_sourceSound)
        {
            var availableSamples = (_audioData.Length + _preSamples) - _position;
            if (availableSamples <= 0) return 0;

            var samplesToCopy = Math.Min(availableSamples, count); //4000

            if (_preSamples == 0)
            {
                _audioData.AsSpan()
                    .Slice(_position, samplesToCopy)
                    .CopyTo(buffer.AsSpan(offset));
                _position += samplesToCopy;
                return samplesToCopy;
            }

            var preLeft = _preSamples - _position;//1200-1000
            if (preLeft <= 0)
            {
                try
                {
                    _audioData.AsSpan()
                        .Slice(_position - _preSamples, samplesToCopy)
                        .CopyTo(buffer.AsSpan(offset));
                }
                catch (Exception ex)
                {
                    throw new Exception("Case: arr", ex);
                }
            }
            else if (preLeft >= samplesToCopy)
            {
                try
                {
                    buffer.AsSpan(offset, samplesToCopy).Fill(0f);
                }
                catch (Exception ex)
                {
                    throw new Exception("Case: num", ex);
                }
            }
            else
            {
                try
                {
                    buffer.AsSpan(offset, preLeft).Fill(0f);
                    _audioData.AsSpan()
                        .Slice(0, samplesToCopy - preLeft)
                        .CopyTo(buffer.AsSpan(offset + preLeft));
                }
                catch (Exception ex)
                {
                    throw new Exception("Case: mix", ex);
                }
            }

            _position += samplesToCopy;
            return samplesToCopy;
        }
    }

    private int TimeSpanToSamples(TimeSpan timeSpan)
    {
        var samples = (int)(timeSpan.TotalSeconds * WaveFormat.SampleRate) * WaveFormat.Channels;
        return samples;
    }

    private TimeSpan SamplesToTimeSpan(int samples)
    {
        return WaveFormat.Channels switch
        {
            1 => TimeSpan.FromSeconds((samples) / (double)WaveFormat.SampleRate),
            2 => TimeSpan.FromSeconds((samples >> 1) / (double)WaveFormat.SampleRate),
            4 => TimeSpan.FromSeconds((samples >> 2) / (double)WaveFormat.SampleRate),
            8 => TimeSpan.FromSeconds((samples >> 3) / (double)WaveFormat.SampleRate),
            _ => TimeSpan.FromSeconds((samples / WaveFormat.Channels) / (double)WaveFormat.SampleRate)
        };
    }
}