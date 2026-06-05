using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio.SoundTouch;

internal sealed class VariableSpeedSampleProvider : ISampleProvider, IDisposable
{
    private readonly ISampleProvider _sourceProvider;
    private readonly SoundTouchProcessor _soundTouch;
    private readonly Lock _gate = new();
    private readonly float[] _sourceReadBuffer;
    private readonly float[] _soundTouchReadBuffer;
    private readonly int _channelCount;
    private float _playbackRate = 1.0f;
    private bool _repositionRequested;
    private bool _disposed;

    public VariableSpeedSampleProvider(
        ISampleProvider sourceProvider,
        int readDurationMilliseconds,
        SoundTouchRateOptions options,
        ILogger logger)
    {
        _sourceProvider = sourceProvider;
        _soundTouch = new SoundTouchProcessor();
        CurrentOptions = options;

        logger.LogDebug("SoundTouch Version {Version}", _soundTouch.VersionString);
        logger.LogDebug("Use QuickSeek: {UseQuickSeek}", _soundTouch.GetUseQuickSeek());
        logger.LogDebug("Use AntiAliasing: {UseAntiAliasing}", _soundTouch.GetUseAntiAliasing());

        SetSoundTouchProfile(options);
        _soundTouch.SetSampleRate(WaveFormat.SampleRate);
        _channelCount = WaveFormat.Channels;
        _soundTouch.SetChannels(_channelCount);
        _sourceReadBuffer = new float[WaveFormat.SampleRate * _channelCount * readDurationMilliseconds / 1000];
        _soundTouchReadBuffer = new float[_sourceReadBuffer.Length * 10];
    }

    public SoundTouchRateOptions CurrentOptions { get; private set; }

    public WaveFormat WaveFormat => _sourceProvider.WaveFormat;

    public float PlaybackRate
    {
        get
        {
            lock (_gate)
            {
                return _playbackRate;
            }
        }
        set
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                if (_playbackRate.Equals(value)) return;
                UpdatePlaybackRate(value);
                _playbackRate = value;
            }
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        lock (_gate)
        {
            if (_disposed) return 0;

            if (_playbackRate.Equals(0))
            {
                Array.Clear(buffer, offset, count);
                return count;
            }

            var alignedCount = count - count % _channelCount;
            if (alignedCount <= 0)
            {
                return 0;
            }

            if (_repositionRequested)
            {
                _soundTouch.Clear();
                _repositionRequested = false;
            }

            var samplesRead = 0;
            var reachedEndOfSource = false;
            var maxOutputFrames = _soundTouchReadBuffer.Length / _channelCount;
            while (samplesRead < alignedCount)
            {
                if (_soundTouch.NumberOfSamplesAvailable == 0)
                {
                    var readFromSource = _sourceProvider.Read(_sourceReadBuffer, 0, _sourceReadBuffer.Length);
                    if (readFromSource > 0)
                    {
                        _soundTouch.PutSamples(_sourceReadBuffer, readFromSource / _channelCount);
                    }
                    else
                    {
                        reachedEndOfSource = true;
                        _soundTouch.Flush();
                    }
                }

                var desiredSampleFrames = Math.Min((alignedCount - samplesRead) / _channelCount, maxOutputFrames);
                if (desiredSampleFrames <= 0)
                {
                    break;
                }

                var received = _soundTouch.ReceiveSamples(_soundTouchReadBuffer, desiredSampleFrames) * _channelCount;
                Array.Copy(_soundTouchReadBuffer, 0, buffer, offset + samplesRead, received);
                samplesRead += received;

                if (received == 0 && reachedEndOfSource) break;
            }

            return samplesRead;
        }
    }

    public void SetSoundTouchProfile(SoundTouchRateOptions options)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (CurrentOptions.PreservePitch != options.PreservePitch && !_playbackRate.Equals(1))
            {
                if (options.PreservePitch)
                {
                    _soundTouch.SetRate(1.0f);
                    _soundTouch.SetPitchOctaves(0f);
                    _soundTouch.SetTempo(_playbackRate);
                }
                else
                {
                    _soundTouch.SetTempo(1.0f);
                    _soundTouch.SetRate(_playbackRate);
                }
            }

            CurrentOptions = options;
            _soundTouch.SetUseAntiAliasing(options.UseAntiAliasing);
            _soundTouch.SetUseQuickSeek(options.UseQuickSeek);
        }
    }

    public void Reposition()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            _repositionRequested = true;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _soundTouch.Dispose();
        }
    }

    private void UpdatePlaybackRate(float value)
    {
        if (value.Equals(0)) return;

        if (CurrentOptions.PreservePitch)
        {
            _soundTouch.SetTempo(value);
        }
        else
        {
            _soundTouch.SetRate(value);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
