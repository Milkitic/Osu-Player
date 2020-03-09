using System;
using System.Diagnostics;

namespace PlayListTest
{
    public class WiseStopwatch : Stopwatch
    {
        private TimeSpan _skipOffset;

        public new void Start()
        {

        }

        public new void Stop()
        {

        }

        public new void Restart()
        {

        }

        public new void Reset()
        {

        }

        public void SkipTo(TimeSpan startOffset)
        {
            _skipOffset = startOffset;
        }

        public new long ElapsedMilliseconds => base.ElapsedMilliseconds + (long)_skipOffset.TotalMilliseconds;
        public new long ElapsedTicks => base.ElapsedTicks + _skipOffset.Ticks;
        public new TimeSpan Elapsed => base.Elapsed + _skipOffset;
    }
}