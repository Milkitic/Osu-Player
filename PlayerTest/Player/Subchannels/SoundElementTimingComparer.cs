using System.Collections.Generic;

namespace PlayerTest.Player.Subchannels
{
    public class SoundElementTimingComparer : IComparer<SoundElement>
    {
        public int Compare(SoundElement x, SoundElement y)
        {
            if (x is null || y is null)
            {
                if (x is null) return -1;
                else
                    return 1;
            }
            return x.Offset.CompareTo(y.Offset);
        }
    }
}