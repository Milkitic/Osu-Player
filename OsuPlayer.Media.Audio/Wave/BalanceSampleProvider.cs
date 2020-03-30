using System;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio.Wave
{
    public class BalanceSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _sourceProvider;
        private int Channels => _sourceProvider.WaveFormat.Channels;
        public BalanceSampleProvider(ISampleProvider sourceProvider)
        {
            _sourceProvider = sourceProvider;
            if (Channels > 2)
                throw new NotSupportedException("channels: " + Channels);
            Balance = 0f;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _sourceProvider.Read(buffer, offset, count);
            if (Channels != 1 && !Balance.Equals(0))
            {
                for (int n = 0; n < count; n += 2)
                {
                    buffer[offset + n] *= (LeftVolume * 2); // left
                    buffer[offset + n + 1] *= (RightVolume * 2); // right
                }
            }

            return samplesRead;
        }

        public float Balance
        {
            get => (RightVolume - LeftVolume) * 2;
            set
            {
                float val;
                if (value > 1.0f)
                {
                    val = 1f;
                }
                else if (value < -1.0f)
                {
                    val = -1f;
                }
                else
                {
                    val = value;
                }

                if (val > 0)
                {
                    LeftVolume = 0.5f - val / 2f;
                    RightVolume = 0.5f + val / 2f;
                }
                else if (val < 0)
                {
                    LeftVolume = 0.5f - val / 2f;
                    RightVolume = 0.5f + val / 2f;
                }
                else
                {
                    LeftVolume = 0.5f;
                    RightVolume = 0.5f;
                }
            }
        }

        public float LeftVolume { get; set; } = 0.5f;
        public float RightVolume { get; set; } = 0.5f;

        public WaveFormat WaveFormat => _sourceProvider.WaveFormat;
    }
}
