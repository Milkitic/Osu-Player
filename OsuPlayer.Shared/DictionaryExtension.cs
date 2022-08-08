using System;
using System.Collections.Generic;
using System.Linq;

namespace Milki.OsuPlayer.Shared
{
    static class DictionaryExtension
    {
        public static List<T> ShuffleToList<T>(this IEnumerable<T> list)
        {
            Random rnd = new Random();
            List<T> copyList = new List<T>(list.ToArray());
            List<T> outputList = new List<T>(copyList.Count);

            while (copyList.Count > 0)
            {
                int rdIndex = rnd.Next(0, copyList.Count - 1);
                T remove = copyList[rdIndex];
                copyList.Remove(remove);
                outputList.Add(remove);
            }
            return outputList;
        }
    }
}
