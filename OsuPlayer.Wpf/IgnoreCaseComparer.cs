using System;
using System.Collections.Generic;
using System.Globalization;

namespace Milky.OsuPlayer
{
    class IgnoreCaseComparer : IEqualityComparer<char>
    {
        public bool Equals(char x, char y)
        {
            throw new NotImplementedException();
        }

        public int GetHashCode(char obj)
        {
            throw new NotImplementedException();
        }
    }

    public static class StringExtension
    {
        public static bool Contains(this string source, string value, bool ignoreSourceCase)
        {
            if (ignoreSourceCase)
                return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, value, CompareOptions.IgnoreCase) >= 0;

            return source.Contains(value);
        }
    }
}
