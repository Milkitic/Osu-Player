using System;

namespace Milky.OsuPlayer.Shared
{
    public class ConcurrentRandom
    {
        private readonly object _lock = new object();
        private Random _random;
        private double _prev;

        public ConcurrentRandom() : this(Environment.TickCount)
        {
        }

        public ConcurrentRandom(int seed)
        {
            Reseed(seed);
        }

        public int Next()
        {
            int val;
            lock (_lock)
            {
                val = _random.Next();
            }
            _prev = val;

            //if (_prev.Equals(val) && val.Equals(0))
            //{
            //    Reseed(Environment.TickCount);
            //    val = Next();
            //    _prev = val;
            //}

            return val;
        }

        public int Next(int maxValue)
        {
            int val;
            lock (_lock)
            {
                val = _random.Next(maxValue);
            }
            _prev = val;

            //if (_prev.Equals(val) && val.Equals(0))
            //{
            //    Reseed(Environment.TickCount);
            //    val = Next(maxValue);
            //    _prev = val;
            //}

            return val;
        }

        public int Next(int minValue, int maxValue)
        {
            int val;
            lock (_lock)
            {
                val = _random.Next(minValue, maxValue);
            }

            //if (_prev.Equals(val) && val.Equals(0))
            //{
            //    Reseed(Environment.TickCount);
            //    val = Next(minValue, maxValue);
            //    _prev = val;
            //}

            return val;
        }

        public void NextBytes(byte[] buffer)
        {
            lock (_lock)
            {
                _random.NextBytes(buffer);
            }
        }

        public double NextDouble()
        {
            double val;
            lock (_lock)
            {
                val = _random.NextDouble();
            }

            //if (_prev.Equals(val) && val.Equals(0))
            //{
            //    Reseed(Environment.TickCount);
            //    val = NextDouble();
            //    _prev = val;
            //}

            return val;
        }

        public double NextDouble(double maxValue) => NextDouble(0, maxValue);

        public double NextDouble(double minValue, double maxValue)
        {
            double val;
            lock (_lock)
            {
                val = _random.NextDouble() * (maxValue - minValue) + minValue;
            }
            return val;
        }

        private void Reseed(int seed)
        {
            lock (_lock)
            {
                _random = new Random(seed);
            }
        }
    }
}
