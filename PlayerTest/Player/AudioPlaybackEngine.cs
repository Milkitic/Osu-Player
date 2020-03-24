using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PlayerTest.Wave;

namespace PlayerTest.Player
{
    public sealed class AudioPlaybackEngine : IDisposable
    {
        private readonly IWavePlayer _outputDevice;
        private readonly VolumeSampleProvider _volumeProvider;

        public static ICollection<string> SupportExtensions { get; } =
            new ReadOnlyCollection<string>(new[] { WavExtension, Mp3Extension, OggExtension });

        public const string WavExtension = ".wav";
        public const string OggExtension = ".ogg";
        public const string Mp3Extension = ".mp3";

        public MixingSampleProvider RootMixer { get; }
        public float RootVolume
        {
            get => _volumeProvider.Volume;
            set => _volumeProvider.Volume = value;
        }

        public AudioPlaybackEngine(IWavePlayer outputDevice)
        {
            RootMixer = new MixingSampleProvider(WaveFormatFactory.WaveFormat);
            _volumeProvider = new VolumeSampleProvider(RootMixer);

            _outputDevice = outputDevice;
            _outputDevice.Init(_volumeProvider);
            _outputDevice.Play();
        }

        public void AddRootSample(ISampleProvider input)
        {
            if (!RootMixer.MixerInputs.Contains(input))
                RootMixer.AddMixerInput(input);
        }

        public void RemoveRootSample(ISampleProvider input)
        {
            if (RootMixer.MixerInputs.Contains(input))
                RootMixer.RemoveMixerInput(input);
        }

        public async Task<ISampleProvider> PlayRootSound(string path, SampleControl sampleControl)
        {
            var rootSample = await RootMixer.PlaySound(path, sampleControl);
            return rootSample;
        }

        public void Dispose()
        {
            _outputDevice?.Dispose();
        }
    }
}