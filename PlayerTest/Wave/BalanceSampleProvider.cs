using NAudio.Wave;

namespace PlayerTest.Wave
{
    public class BalanceSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _sourceProvider;
        public BalanceSampleProvider(ISampleProvider sourceProvider)
        {
            _sourceProvider = sourceProvider;
            Balance = 0f;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _sourceProvider.Read(buffer, offset, count);
            if (!Balance.Equals(0) )
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
