using System.Diagnostics;

namespace OsbPlayerTest
{
    public class Timing
    {
        public long Offset => ControlOffset + Watch.ElapsedMilliseconds;
        public long ControlOffset;
        public readonly Stopwatch Watch;

        public void SetTiming(long time)
        {
            ControlOffset = time;
            Watch.Reset();
        }

        public Timing(long controlOffset, Stopwatch watch)
        {
            ControlOffset = controlOffset;
            Watch = watch;
        }
    }
}