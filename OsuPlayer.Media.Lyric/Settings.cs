namespace Milki.OsuPlayer.Media.Lyric
{
    public static class Settings
    {
        public static bool DebugMode { get; set; } = false;
        public static bool EnableOutputSearchResult { get; set; } = false;
        public static bool PreferTranslateLyrics { get; set; } = false;
        public static string LyricsSource { get; set; } = "auto";
        public static string LyricsSentenceOutputPath { get; set; } = @"..\lyric.txt";
        //内部保留
        public static bool IsUsedByPlugin { get; internal set; } = false;
        public static bool BothLyrics { get; internal set; } = true;
        public static int SearchAndDownloadTimeout { get; internal set; } = 2000;
        public static int GobalTimeOffset { get; internal set; } = 0;
        public static uint ForceKeepTime { get; internal set; } = 0;
        public static bool StrictMatch { get; set; } = true;

        /// <summary>
        /// 下一版本会钦定用动态形式的(如果那个没问题的话)
        /// </summary>
        public static bool UseStaticLyricsCombine { get; internal set; } = false;
    }
}
