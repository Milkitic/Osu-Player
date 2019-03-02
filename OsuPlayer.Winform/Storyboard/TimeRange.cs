using System.Linq;

namespace Milkitic.OsuPlayer.Storyboard
{
    public struct TimeRange
    {
        public int Max { get; set; }
        public int Min { get; set; }

        public static TimeRange Default => new TimeRange { Max = int.MaxValue, Min = int.MinValue };

        public static int GetMaxTime(params TimeRange[] timeRanges)
        {
            var maxTime = timeRanges.Where(t => t.Max != int.MaxValue).ToArray();
            return maxTime.Length == 0 ? 0 : maxTime.Max(t => t.Max);
        }

        public static int GetMinTime(params TimeRange[] timeRanges)
        {
            var minTime = timeRanges.Where(t => t.Min != int.MinValue).ToArray();
            return minTime.Length == 0 ? 0 : minTime.Min(t => t.Min);
        }
    }
}