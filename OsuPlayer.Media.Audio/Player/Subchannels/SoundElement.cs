using System.Threading.Tasks;
using Coosu.Beatmap.Sections.HitObject;
using Milky.OsuPlayer.Media.Audio.Wave;

namespace Milky.OsuPlayer.Media.Audio.Player.Subchannels
{
    public sealed class SoundElement
    {
        private CachedSound _cachedSound;
        private SoundElement() { }

        public double Offset { get; private set; }
        public double NearlyPlayEndTime => GetCachedSoundAsync().Result.Duration.TotalMilliseconds + Offset;
        public float Volume { get; private set; }
        public float Balance { get; private set; }
        public string FilePath { get; private set; }
        public HitsoundType SlideType { get; private set; }
        public SlideControlType ControlType { get; private set; } = SlideControlType.None;

        internal async Task<CachedSound> GetCachedSoundAsync()
        {
            if (_cachedSound != null)
                return _cachedSound;

            var result = await CachedSound.GetOrCreateCacheSound(FilePath).ConfigureAwait(false);
            _cachedSound = result;
            return result;
        }

        public static SoundElement Create(double offset, float volume, float balance, string filePath)
        {
            var se = new SoundElement
            {
                Offset = offset,
                Volume = volume,
                Balance = balance,
                FilePath = filePath,
            };

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