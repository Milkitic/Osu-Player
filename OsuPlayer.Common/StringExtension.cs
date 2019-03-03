using System.Globalization;

namespace Milky.OsuPlayer.Common
{
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