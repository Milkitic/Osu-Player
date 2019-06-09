﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Milky.OsuPlayer.Media.Audio.Music.SampleProviders;
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

        public bool Enable3dEffect { get; } = true;

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

        public void PlaySound(string path, float volume, float balance = 0f)
        {
            if (!_cachedDictionary.ContainsKey(path))
            {
                CreateCacheSound(path);
            }

            PlaySound(_cachedDictionary[path], volume, balance);
        }

        private void PlaySound(CachedSound sound, float volume, float balance)
        {
            AddMixerInput(new CachedSoundSampleProvider(sound), volume, balance);
        }

        private void AddMixerInput(ISampleProvider input, float volume, float balance)
        {
            var volumed = AdjustVolume(input, volume);
            if (Enable3dEffect)
            {
                var balanced = AdjustBalance(volumed, balance);
                _mixer.AddMixerInput(balanced);
            }
            else
            {
                _mixer.AddMixerInput(volumed);
            }
        }

        private ISampleProvider AdjustVolume(ISampleProvider input, float volume)
        {
            var volumeSampleProvider = new VolumeSampleProvider(input)
            {
                Volume = volume
            };
            return volumeSampleProvider;
        }
        private ISampleProvider AdjustBalance(ISampleProvider input, float balance)
        {
            var volumeSampleProvider = new ChannelSampleProvider(input)
            {
                Balance = balance
            };
            return volumeSampleProvider;
        }

        public void Dispose()
        {
            _outputDevice?.Dispose();
        }
    }
}