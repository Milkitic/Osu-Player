using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milky.OsuPlayer
{
    public static class RandomSeed
    {
        private static readonly Random Random = new Random();
        public static double RandomNumber => Random.NextDouble();
    }
}
