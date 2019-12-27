using System;

namespace Milky.OsuPlayer.Media.Audio.Core
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
