using System;
using KeyAsio.Core.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio.SoundTouch;

internal sealed class SoundTouchPlaybackRateProcessorFactory : IPlaybackRateProcessorFactory
{
    private readonly int _readDurationMilliseconds;
    private readonly bool _useAntiAliasing;
    private readonly bool _useQuickSeek;
    private readonly ILogger<SoundTouchPlaybackRateProcessorFactory> _logger;

    public SoundTouchPlaybackRateProcessorFactory(
        int readDurationMilliseconds = 10,
        bool useAntiAliasing = false,
        bool useQuickSeek = true,
        ILogger<SoundTouchPlaybackRateProcessorFactory>? logger = null)
    {
        if (readDurationMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(readDurationMilliseconds), readDurationMilliseconds, "Read duration must be positive.");
        }

        _readDurationMilliseconds = readDurationMilliseconds;
        _useAntiAliasing = useAntiAliasing;
        _useQuickSeek = useQuickSeek;
        _logger = logger ?? NullLogger<SoundTouchPlaybackRateProcessorFactory>.Instance;
    }

    public bool IsSupported => true;

    public IPlaybackRateProcessor Create(ISampleProvider source, PlaybackRateState initialState)
    {
        return new SoundTouchPlaybackRateProcessor(
            source,
            initialState,
            _readDurationMilliseconds,
            _useAntiAliasing,
            _useQuickSeek,
            _logger);
    }

    private sealed class SoundTouchPlaybackRateProcessor : IPlaybackRateProcessor
    {
        private readonly VariableSpeedSampleProvider _provider;
        private PlaybackRateState _rateState;

        public SoundTouchPlaybackRateProcessor(
            ISampleProvider source,
            PlaybackRateState initialState,
            int readDurationMilliseconds,
            bool useAntiAliasing,
            bool useQuickSeek,
            ILogger logger)
        {
            _provider = new VariableSpeedSampleProvider(
                source,
                readDurationMilliseconds,
                new SoundTouchRateOptions(initialState.PreservePitch, useAntiAliasing, useQuickSeek),
                logger)
            {
                PlaybackRate = initialState.Rate
            };
            _rateState = initialState;
        }

        public PlaybackRateState RateState
        {
            get => _rateState;
            set
            {
                _rateState = value;
                _provider.PlaybackRate = value.Rate;
                _provider.SetSoundTouchProfile(new SoundTouchRateOptions(
                    value.PreservePitch,
                    _provider.CurrentOptions.UseAntiAliasing,
                    _provider.CurrentOptions.UseQuickSeek));
            }
        }

        public WaveFormat WaveFormat => _provider.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            return _provider.Read(buffer, offset, count);
        }

        public void Reposition()
        {
            _provider.Reposition();
        }

        public void Dispose()
        {
            _provider.Dispose();
        }
    }
}
