using System;
using OSharp.Common;

namespace Milky.OsuPlayer
{
    public static class RandomSeed
    {
        private static readonly ConcurrentRandom Random = new ConcurrentRandom();
        public static double RandomNumber => Random.NextDouble();
    }
}
