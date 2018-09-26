using System.Diagnostics;

namespace OsbPlayerTest
{
    public class Timing
    {
        public float Offset => ControlOffset + Watch.ElapsedMilliseconds * PlayBack;
        public long ControlOffset;
        public readonly Stopwatch Watch;
        public float PlayBack { get; set; } = 1f;

        public Timing(long controlOffset, Stopwatch watch)
        {
            ControlOffset = controlOffset;
            Watch = watch;
        }

        public void SetTiming(long time)
        {
            ControlOffset = time;
            Watch.Reset();
        }

        public void Reset()
        {
            ControlOffset = 0;
            Watch.Stop();
            Watch.Reset();
        }

        public void Pause()
        {
            Watch.Stop();
        }
        public void Start()
        {
            Watch.Start();
        }
    }
}