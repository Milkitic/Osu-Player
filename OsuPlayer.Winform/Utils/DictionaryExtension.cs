using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Utils
{
    static class DictionaryExtension
    {
        public static IEnumerable<TKey> RandomKeys<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            Random rnd = new Random();
            List<TKey> values = dict.Keys.ToList();
            int size = dict.Count;
            while (true)
            {
                yield return values[rnd.Next(size)];
            }
        }

        public static IEnumerable<TValue> RandomValues<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            Random rnd = new Random();
            List<TValue> values = dict.Values.ToList();
            int size = dict.Count;
            while (true)
            {
                yield return values[rnd.Next(size)];
            }
        }
    }
}
