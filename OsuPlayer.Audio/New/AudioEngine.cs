using Milki.Extensions.MixPlayer.Devices;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.OsuPlayer.Audio.New;

public class AudioEngine : AudioPlaybackEngine
{
    private EnhancedVolumeSampleProvider _hitsoundVolumeSampleProvider = null!;
    private EnhancedVolumeSampleProvider _musicVolumeSampleProvider = null!;

    public AudioEngine(IWavePlayer? outputDevice, int sampleRate = 44100, int channelCount = 2)
        : base(outputDevice, sampleRate, channelCount, false, true)
    {
        InitializeMixers();
    }

    public AudioEngine(DeviceDescription? deviceDescription, int sampleRate = 44100, int channelCount = 2)
        : base(deviceDescription, sampleRate, channelCount, false, true)
    {
        InitializeMixers();
    }

    public MixingSampleProvider EffectMixer { get; private set; } = null!;
    public MixingSampleProvider MusicMixer { get; private set; } = null!;

    public float EffectVolume
    {
        get => _hitsoundVolumeSampleProvider.Volume;
        set
        {
            if (value.Equals(_hitsoundVolumeSampleProvider.Volume)) return;
            _hitsoundVolumeSampleProvider.Volume = value;
            OnPropertyChanged();
        }
    }

    public float MusicVolume
    {
        get => _musicVolumeSampleProvider.Volume;
        set
        {
            if (value.Equals(_musicVolumeSampleProvider.Volume)) return;
            _musicVolumeSampleProvider.Volume = value;
            OnPropertyChanged();
        }
    }

    private void InitializeMixers()
    {
        EffectMixer = new MixingSampleProvider(WaveFormat)
        {
            ReadFully = true
        };
        _hitsoundVolumeSampleProvider = new EnhancedVolumeSampleProvider(EffectMixer)
        {
            Volume = 1f
        };
        RootMixer.AddMixerInput(_hitsoundVolumeSampleProvider);

        MusicMixer = new MixingSampleProvider(WaveFormat)
        {
            ReadFully = true
        };
        _musicVolumeSampleProvider = new EnhancedVolumeSampleProvider(MusicMixer)
        {
            Volume = 1f
        };
        RootMixer.AddMixerInput(_musicVolumeSampleProvider);
    }
}