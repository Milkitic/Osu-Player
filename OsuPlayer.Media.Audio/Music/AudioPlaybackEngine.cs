using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio.Music.SampleProviders;
using Milky.OsuPlayer.Media.Audio.Music.WaveProviders;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OsuPlayer.Devices;

namespace Milky.OsuPlayer.Media.Audio.Music
{
    public class AudioPlaybackEngine : IDisposable
    {
        static ConcurrentDictionary<string, CachedSound> _cachedDictionary = new ConcurrentDictionary<string, CachedSound>();

        private readonly IWavePlayer _outputDevice;
        private VolumeSampleProvider _hitsoundVolumeProvider;

        private static string[] _supportExtensions = { ".wav", ".mp3", ".ogg" };
        private readonly MixingSampleProvider _rootMixer;
        private MixingSampleProvider _hitsoundMixer;
        private MixingSampleProvider _sampleMixer;
        private VolumeSampleProvider _sampleVolumeProvider;

        public bool Enable3dEffect { get; } = true;

        public float HitsoundVolume
        {
            get => _hitsoundVolumeProvider.Volume;
            set => _hitsoundVolumeProvider.Volume = value;
        }
        public float SampleVolume
        {
            get => _sampleVolumeProvider.Volume;
            set => _sampleVolumeProvider.Volume = value;
        }

        public AudioPlaybackEngine(IWavePlayer outputDevice, int sampleRate = 44100, int channelCount = 2)
        {
            _outputDevice = outputDevice;
            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount);
            _rootMixer = new MixingSampleProvider(waveFormat)
            {
                ReadFully = true
            };

            _hitsoundMixer = new MixingSampleProvider(waveFormat)
            {
                ReadFully = true
            };

            _sampleMixer = new MixingSampleProvider(waveFormat)
            {
                ReadFully = true
            };

            _hitsoundVolumeProvider = new VolumeSampleProvider(_hitsoundMixer);
            _sampleVolumeProvider = new VolumeSampleProvider(_sampleMixer);

            _rootMixer.AddMixerInput(_hitsoundVolumeProvider);
            _rootMixer.AddMixerInput(_sampleVolumeProvider);

            _outputDevice.Init(_rootMixer);
            _outputDevice.Play();
        }

        public void CreateCacheSounds(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                CreateCacheSound(path); // Cache each file once before play.
            }
        }

        public CachedSound GetOrCreateCacheSound(string path)
        {
            if (!_cachedDictionary.ContainsKey(path))
            {
                CreateCacheSound(path);
            }

            return _cachedDictionary[path];
        }

        public void CreateCacheSound(string path)
        {
            string newPath = path;
            if (!File.Exists(newPath))
            {
                newPath = TryGetPath(newPath);
            }

            if (!File.Exists(newPath))
            {
                newPath = TryGetPath(Path.Combine(Path.GetDirectoryName(newPath), Path.GetFileNameWithoutExtension(newPath)));
            }

            if (_cachedDictionary.ContainsKey(path))
            {
                return;
            }

            if (!File.Exists(newPath))
            {
                _cachedDictionary.TryAdd(path, null);
                return;
            }

            try
            {
                _cachedDictionary.TryAdd(path, new CachedSound(newPath)); // Cache each file once before play.
            }
            catch
            {
                _cachedDictionary.TryAdd(path, null);
            }
        }

        private static string TryGetPath(string path)
        {
            foreach (var ext in _supportExtensions)
            {
                var autoAudioFile = path + ext;
                if (!File.Exists(autoAudioFile))
                    continue;

                path = autoAudioFile;
                break;
            }

            return path;
        }

        public void PlaySound(string path, float volume, float balance = 0f, bool isHitsound = true)
        {
            if (!_cachedDictionary.ContainsKey(path))
            {
                CreateCacheSound(path);
            }

            PlaySound(_cachedDictionary[path], volume, balance, isHitsound);
        }

        public void AddRootSample(ISampleProvider input)
        {
            if (!_rootMixer.MixerInputs.Contains(input))
                _rootMixer.AddMixerInput(input);
        }

        public void RemoveRootSample(ISampleProvider input)
        {
            if (_rootMixer.MixerInputs.Contains(input))
                _rootMixer.RemoveMixerInput(input);
        }

        public void AddHitsoundSample(ISampleProvider input)
        {
            if (!_hitsoundMixer.MixerInputs.Contains(input))
                _hitsoundMixer.AddMixerInput(input);
        }

        public void RemoveHitsoundSample(ISampleProvider input)
        {
            if (_hitsoundMixer.MixerInputs.Contains(input))
                _hitsoundMixer.RemoveMixerInput(input);
        }

        private void PlaySound(CachedSound sound, float volume, float balance, bool isHitsound)
        {
            if (sound == null) return;
            AddMixerInput(new CachedSoundSampleProvider(sound), volume, balance, isHitsound);
        }

        private void AddMixerInput(ISampleProvider input, float volume, float balance, bool isHitsound)
        {
            var volumed = AdjustVolume(input, volume);
            if (Enable3dEffect)
            {
                var balanced = AdjustBalance(volumed, balance);
                if (isHitsound)
                    _hitsoundMixer.AddMixerInput(balanced);
                else
                    _sampleMixer.AddMixerInput(balanced);
            }
            else
            {
                if (isHitsound)
                    _hitsoundMixer.AddMixerInput(volumed);
                else
                    _sampleMixer.AddMixerInput(volumed);
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

        public static void ClearCacheSounds()
        {
            _cachedDictionary.Clear();
        }
    }
}