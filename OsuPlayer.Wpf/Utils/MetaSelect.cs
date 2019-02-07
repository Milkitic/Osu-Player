namespace Milky.OsuPlayer.Utils
{
    public static class MetaSelect
    {

        public static string GetUnicode(string ori, string unicode)
        {
            return string.IsNullOrEmpty(unicode)
                ? (string.IsNullOrEmpty(ori) ? "" : ori)
                : unicode;
        }

        public static string GetOriginal(string ori, string unicode)
        {
            return string.IsNullOrEmpty(ori)
                ? (string.IsNullOrEmpty(unicode) ? "" : unicode)
                : ori;
        }
    }
}
