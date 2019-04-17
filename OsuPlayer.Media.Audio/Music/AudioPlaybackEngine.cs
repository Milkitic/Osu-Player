using System;
using Milky.OsuPlayer.Media.Audio.Music.WaveProviders;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milky.OsuPlayer.Media.Audio.Music
{
    public class AudioPlaybackEngine : IDisposable
    {
        private readonly IWavePlayer _outputDevice;
        private readonly MixingSampleProvider _mixer;

        public AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 2)
        {
            _outputDevice = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 5);
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
            {
                ReadFully = true
            };

            _outputDevice.Init(_mixer);
            _outputDevice.Play();
        }

        public void PlaySound(CachedSound sound, float volume)
        {
            AddMixerInput(new CachedSoundSampleProvider(sound), volume);
        }

        private void AddMixerInput(ISampleProvider input, float volume)
        {
            _mixer.AddMixerInput(AdjustVolume(input, volume));
        }

        private ISampleProvider AdjustVolume(ISampleProvider input, float volume)
        {
            var volumeSampleProvider = new VolumeSampleProvider(input)
            {
                Volume = volume
            };
            return volumeSampleProvider;
        }

        public void Dispose()
        {
            _outputDevice?.Dispose();
        }
    }
}