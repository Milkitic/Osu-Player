using System.Threading.Tasks;
using PlayerTest.Wave;

namespace PlayerTest.Player.Channel
{
    public sealed class SoundElement
    {
        private SoundElement() { }

        public double Offset { get; private set; }
        internal double NearlyPlayEndTime => CachedSound.Duration.TotalMilliseconds + Offset;
        public float Volume { get; private set; }
        public float Balance { get; private set; }
        public string FilePath { get; private set; }
        public SlideType SlideType { get; private set; }
        public SlideControlType ControlType { get; private set; } = SlideControlType.None;

        internal CachedSound CachedSound { get; private set; }

        public async Task<SoundElement> CreateAsync(double offset, float volume, float balance, string filePath)
        {
            var se = new SoundElement
            {
                Offset = offset,
                Volume = volume,
                Balance = balance,
                FilePath = filePath,
                CachedSound = await CachedSound.GetOrCreateCacheSound(filePath)
            };

            return se;
        }

        public async Task<SoundElement> CreateSlideSignalAsync(double offset, float volume, float balance,
            string filePath, SlideType slideType)
        {
            return new SoundElement
            {
                Offset = offset,
                Volume = volume,
                Balance = balance,
                FilePath = filePath,
                CachedSound = await CachedSound.GetOrCreateCacheSound(filePath),
                ControlType = SlideControlType.StartNew,
                SlideType = slideType
            };
        }

        public SoundElement CreateStopSignal(double offset)
        {
            return new SoundElement
            {
                Offset = offset,
                ControlType = SlideControlType.StopRunning
            };
        }

        public SoundElement CreateVolumeSignal(double offset, float volume)
        {
            return new SoundElement
            {
                Offset = offset,
                Volume = volume,
                ControlType = SlideControlType.ChangeVolume
            };
        }

        public SoundElement CreateBalanceSignal(double offset, float balance)
        {
            return new SoundElement
            {
                Offset = offset,
                Balance = balance,
                ControlType = SlideControlType.ChangeBalance
            };
        }
    }

    public enum SlideType
    {
        None, Slide, Addition
    }
}