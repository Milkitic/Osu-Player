using System;

namespace PlayerTest.Player
{
    public class SampleControl
    {
        internal Action<float> VolumeChanged { get; set; }
        internal Action<float> BalanceChanged { get; set; }

        private float _volume;
        private float _balance;

        public float Volume
        {
            get => _volume;
            set
            {
                if (Equals(_volume, value)) return;
                _volume = value;
                VolumeChanged?.Invoke(value);
            }
        }

        public float Balance
        {
            get => _balance;
            set
            {
                if (Equals(_balance, value)) return;
                _balance = value;
                BalanceChanged?.Invoke(value);
            }
        }
    }
}