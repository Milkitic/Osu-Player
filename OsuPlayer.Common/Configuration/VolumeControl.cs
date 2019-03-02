namespace Milky.OsuPlayer.Common.Configuration
{
    public class VolumeControl
    {
        private float _main = 0.8f;
        private float _bgm = 1;
        private float _hs = 0.9f;

        public float Main { get => _main; set => SetValue(ref _main, value); }
        public float Music { get => _bgm; set => SetValue(ref _bgm, value); }
        public float Hitsound { get => _hs; set => SetValue(ref _hs, value); }

        private static void SetValue(ref float source, float value) => source = value < 0 ? 0 : (value > 1 ? 1 : value);
    }
}