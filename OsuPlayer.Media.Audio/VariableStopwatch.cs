using System;
using System.Diagnostics;

namespace Milky.OsuPlayer.Media.Audio
{
    public class VariableStopwatch : Stopwatch
    {
        public TimeSpan ManualOffset { get; set; }
        public TimeSpan VariableOffset { get; set; }
        public TimeSpan CalibrationOffset { get; set; }

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
            (long)(base.Elapsed.TotalMilliseconds * Rate +
                   _skipOffset.TotalMilliseconds +
                   ManualOffset.TotalMilliseconds +
                   VariableOffset.TotalMilliseconds +
                   CalibrationOffset.TotalMilliseconds);

        public new long ElapsedTicks =>
            (long)(base.ElapsedTicks * Rate +
                   _skipOffset.Ticks +
                   ManualOffset.Ticks +
                   VariableOffset.Ticks +
                   CalibrationOffset.Ticks);

        public new TimeSpan Elapsed =>
            TimeSpan.FromMilliseconds(base.Elapsed.TotalMilliseconds * Rate)
                .Add(_skipOffset)
                .Add(ManualOffset)
                .Add(VariableOffset)
                .Add(CalibrationOffset);
    }
}