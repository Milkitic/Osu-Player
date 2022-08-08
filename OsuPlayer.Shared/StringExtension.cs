using System.Globalization;

namespace Milki.OsuPlayer.Shared
{
    public static class StringExtension
    {
        public static bool Contains(this string source, string value, bool ignoreSourceCase)
        {
            if (source is null)
            {
                return false;
            }

            if (ignoreSourceCase)
            {
                return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, value, CompareOptions.IgnoreCase) >= 0;
            }

            return source.Contains(value);
        }
    }
}