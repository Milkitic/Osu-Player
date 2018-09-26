using System.Collections.Generic;

namespace Milkitic.OsuPlayer.Wpf
{
    public class Config
    {
        public int DesiredLatency { get; set; } = 80;
        public string DbPath { get; set; }
        public VolumeControl Volume { get; set; } = new VolumeControl();
        public OffsetControl OffsetControl { get; set; } = new OffsetControl();
    }

    public class VolumeControl
    {
        private float _main = 0.7f;
        private float _bgm = 1;
        private float _hs = 1;

        public float Main { get => _main; set => SetValue(ref _main, value); }
        public float Music { get => _bgm; set => SetValue(ref _bgm, value); }
        public float Hitsound { get => _hs; set => SetValue(ref _hs, value); }

        private static void SetValue(ref float source, float value)
        {
            if (value < 0) source = 0;
            else if (value > 1) source = 1;
            else source = value;
        }
    }
    public class OffsetControl
    {
        public int GeneralOffset { get; set; } = 85;
        public Dictionary<string, int> OffsetList { get; set; } = new Dictionary<string, int>();
    }
}
