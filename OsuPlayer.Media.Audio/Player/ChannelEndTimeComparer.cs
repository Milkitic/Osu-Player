using System.Collections.Generic;

namespace Milky.OsuPlayer.Media.Audio.Player
{
    internal class ChannelEndTimeComparer : IComparer<Subchannel>
    {
        public int Compare(Subchannel x, Subchannel y)
        {
            if (x is null && y is null)
                return 0;
            if (y is null)
                return 1;
            if (x is null)
                return -1;

            var o = (x.ChannelEndTime).CompareTo(y.ChannelEndTime);
            if (o == 0) return 1;
            return o;
        }
    }
}