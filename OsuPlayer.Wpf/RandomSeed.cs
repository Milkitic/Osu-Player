using System;

namespace Milky.OsuPlayer
{
    public static class RandomSeed
    {
        private static readonly Random Random = new Random();
        public static double RandomNumber => Random.NextDouble();
    }
}
