using System;

namespace Milky.OsuPlayer.Media.Audio.Core
{
    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(TimeSpan position, TimeSpan duration)
        {
            Position = position;
            Duration = duration;
        }

        public TimeSpan Position { get; }
        public TimeSpan Duration { get; }
    }
}
