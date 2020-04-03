using Milky.OsuPlayer.Media.Audio.Wave;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Player
{
    public static class MixingSampleProviderExtension
    {
        internal static ISampleProvider PlaySound(this MixingSampleProvider mixer, CachedSound sound,
            SampleControl sampleControl)
        {
            PlaySound(mixer, sound, sampleControl, out var rootSample);
            return rootSample;
        }

        internal static ISampleProvider PlaySound(this MixingSampleProvider mixer, CachedSound sound,
            float volume, float balance)
        {
            PlaySound(mixer, sound, volume, balance, out var rootSample);
            return rootSample;
        }

        public static async Task<ISampleProvider> PlaySound(this MixingSampleProvider mixer, string path,
            SampleControl sampleControl)
        {
            var sound = await CachedSound.GetOrCreateCacheSound(path).ConfigureAwait(false);
            PlaySound(mixer, sound, sampleControl, out var rootSample);
            return rootSample;
        }

        public static async Task<ISampleProvider> PlaySound(this MixingSampleProvider mixer, string path,
            float volume, float balance)
        {
            var sound = await CachedSound.GetOrCreateCacheSound(path).ConfigureAwait(false);
            PlaySound(mixer, sound, volume, balance, out var rootSample);
            return rootSample;
        }

        public static void AddMixerInput(this MixingSampleProvider mixer, ISampleProvider input,
            SampleControl sampleControl, out ISampleProvider rootSample)
        {
            var adjustVolume = input.AddToAdjustVolume(sampleControl.Volume);
            var adjustBalance = adjustVolume.AddToBalanceProvider(sampleControl.Balance);

            sampleControl.VolumeChanged = f => adjustVolume.Volume = f;
            sampleControl.BalanceChanged = f => adjustBalance.Balance = f;

            rootSample = adjustBalance;
            mixer.AddMixerInput(adjustBalance);
        }

        public static void AddMixerInput(this MixingSampleProvider mixer, ISampleProvider input,
            float volume, float balance, out ISampleProvider rootSample)
        {
            var adjustVolume = input.AddToAdjustVolume(volume);
            var adjustBalance = adjustVolume.AddToBalanceProvider(balance);

            rootSample = adjustBalance;
            mixer.AddMixerInput(adjustBalance);
        }

        private static void PlaySound(MixingSampleProvider mixer, CachedSound sound, SampleControl sampleControl,
            out ISampleProvider rootSample)
        {
            if (sound == null)
            {
                rootSample = default;
                return;
            }

            mixer.AddMixerInput(new CachedSoundSampleProvider(sound), sampleControl, out rootSample);
        }

        private static void PlaySound(MixingSampleProvider mixer, CachedSound sound, float volume, float balance,
            out ISampleProvider rootSample)
        {
            if (sound == null)
            {
                rootSample = default;
                return;
            }

            mixer.AddMixerInput(new CachedSoundSampleProvider(sound), volume, balance, out rootSample);
        }

        private static VolumeSampleProvider AddToAdjustVolume(this ISampleProvider input, float volume)
        {
            var volumeSampleProvider = new VolumeSampleProvider(input)
            {
                Volume = volume
            };
            return volumeSampleProvider;
        }

        private static BalanceSampleProvider AddToBalanceProvider(this ISampleProvider input, float balance)
        {
            var volumeSampleProvider = new BalanceSampleProvider(input)
            {
                Balance = balance
            };
            return volumeSampleProvider;
        }
    }
}