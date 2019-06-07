using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Milky.OsuPlayer.Media.Audio.Music.WaveProviders;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milky.OsuPlayer.Media.Audio.Music
{
    public class AudioPlaybackEngine : IDisposable
    {
        ConcurrentDictionary<string, CachedSound> _cachedDictionary = new ConcurrentDictionary<string, CachedSound>();

        private readonly IWavePlayer _outputDevice;
        private readonly MixingSampleProvider _mixer;
        private VolumeSampleProvider _volumeProvider;

        public float Volume
        {
            get => _volumeProvider.Volume;
            set => _volumeProvider.Volume = value;
        }

        public AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 2)
        {
            _outputDevice = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 5);
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
            {
                ReadFully = true
            };
            _volumeProvider = new VolumeSampleProvider(_mixer);
            _outputDevice.Init(_volumeProvider);
            _outputDevice.Play();
        }

        public void CreateCacheSounds(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                CreateCacheSound(path); // Cache each file once before play.
            }
        }

        public void CreateCacheSound(string path)
        {
            _cachedDictionary.TryAdd(path, new CachedSound(path)); // Cache each file once before play.
        }

        public void PlaySound(string path, float volume)
        {
            if (!_cachedDictionary.ContainsKey(path))
            {
                CreateCacheSound(path);
            }

            PlaySound(_cachedDictionary[path], volume);
        }

        private void PlaySound(CachedSound sound, float volume)
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