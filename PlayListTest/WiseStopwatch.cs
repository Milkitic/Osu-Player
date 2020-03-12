using System;
using System.Diagnostics;

namespace PlayListTest
{
    public class WiseStopwatch : Stopwatch
    {
        private TimeSpan _skipOffset;
        
        public new void Restart()
        {
            _skipOffset = TimeSpan.Zero;
            base.Restart();
        }

        public new void Reset()
        {
            _skipOffset = TimeSpan.Zero;
            base.Reset();
        }

        public void SkipTo(TimeSpan startOffset)
        {
            _skipOffset = startOffset;
            Restart();
        }

        public new long ElapsedMilliseconds => base.ElapsedMilliseconds + (long)_skipOffset.TotalMilliseconds;
        public new long ElapsedTicks => base.ElapsedTicks + _skipOffset.Ticks;
        public new TimeSpan Elapsed => base.Elapsed + _skipOffset;
    }
}