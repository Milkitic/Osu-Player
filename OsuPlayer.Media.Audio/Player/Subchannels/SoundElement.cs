using System;
using System.Threading.Tasks;
using Milky.OsuPlayer.Media.Audio.Wave;
using NAudio.Wave;
using OSharp.Beatmap.Sections.HitObject;

namespace Milky.OsuPlayer.Media.Audio.Player.Subchannels
{
    public sealed class SoundElement
    {
        private CachedSound _cachedSound;
        private SoundElement() { }

        public double Offset { get; private set; }
        public double NearlyPlayEndTime
        {
            get
            {
                var cachedSound = GetCachedSoundAsync().Result;
                if (cachedSound == null) return 0;
                return cachedSound.Duration.TotalMilliseconds + Offset;
            }
        }

        public float Volume { get; private set; }
        public float Balance { get; private set; }
        public string FilePath { get; private set; }
        public HitsoundType SlideType { get; private set; }
        public SlideControlType ControlType { get; private set; } = SlideControlType.None;
        internal ISampleProvider? RelatedProvider { get; set; }
        internal SoundElement? SubSoundElement { get; private set; }

        internal async Task<CachedSound> GetCachedSoundAsync()
        {
            if (_cachedSound != null)
                return _cachedSound;

            var result = await CachedSound.GetOrCreateCacheSound(FilePath).ConfigureAwait(false);
            _cachedSound = result;
            return result;
        }

        public static SoundElement Create(double offset, float volume, float balance, string filePath, double? forceStopOffset = null)
        {
            var se = new SoundElement
            {
                Offset = offset,
                Volume = volume,
                Balance = balance,
                FilePath = filePath,
            };

            if (forceStopOffset != null)
            {
                se.SubSoundElement = new SoundElement
                {
                    Offset = forceStopOffset.Value,
                    ControlType = SlideControlType.StopNote
                };
            }

            return se;
        }

        public static SoundElement CreateSlideSignal(double offset, float volume, float balance,
            string filePath, HitsoundType slideType)
        {
            return new SoundElement
            {
                Offset = offset,
                Volume = volume,
                Balance = balance,
                FilePath = filePath,
                ControlType = SlideControlType.StartNew,
                SlideType = slideType
            };
        }

        public static SoundElement CreateStopSignal(double offset)
        {
            return new SoundElement
            {
                Offset = offset,
                ControlType = SlideControlType.StopRunning
            };
        }

        public static SoundElement CreateVolumeSignal(double offset, float volume)
        {
            return new SoundElement
            {
                Offset = offset,
                Volume = volume,
                ControlType = SlideControlType.ChangeVolume
            };
        }

        public static SoundElement CreateBalanceSignal(double offset, float balance)
        {
            return new SoundElement
            {
                Offset = offset,
                Balance = balance,
                ControlType = SlideControlType.ChangeBalance
            };
        }
    }
}