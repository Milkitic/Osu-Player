﻿using System;
using System.Diagnostics;

namespace Milky.OsuPlayer.Shared
{
    public class VariableStopwatch : Stopwatch
    {
        public float Rate
        {
            get => _rate;
            set
            {
                SkipTo(Elapsed);
                _rate = value;
            }
        }

        private TimeSpan _skipOffset;
        private float _rate = 1;

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
            if (IsRunning)
                base.Restart();
        }

        public new long ElapsedMilliseconds =>
            (long)(base.Elapsed.TotalMilliseconds * Rate + _skipOffset.TotalMilliseconds);

        public new long ElapsedTicks =>
            (long)(base.ElapsedTicks * Rate + _skipOffset.Ticks);

        public new TimeSpan Elapsed =>
            TimeSpan.FromMilliseconds(base.Elapsed.TotalMilliseconds * Rate).Add(_skipOffset);
    }
}