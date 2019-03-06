using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Music
{
    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(long position, long duration)
        {
            Position = position;
            Duration = duration;
        }

        public long Position { get; }
        public long Duration { get; }
    }
}
