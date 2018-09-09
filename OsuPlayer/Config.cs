using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer
{
    public class Config
    {
        public int Offset { get; set; } = 85;
        public VolumeControl Volume { get; set; } = new VolumeControl();
        public int DesiredLatency { get; set; } = 80;
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
}
