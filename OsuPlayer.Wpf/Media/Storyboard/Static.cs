namespace Milky.OsuPlayer.Media.Storyboard
{
    public struct Static<T>
    {
        public T Source { get; set; }
        public T RealTime { get; set; }
        public T Target { get; set; }

        public static explicit operator Static<T>(T value) => new Static<T>
        {
            Source = value,
            RealTime = value,
            Target = value
        };

        public static explicit operator T(Static<T> value) => value.Source;

        public void TargetToRealTime() => Target = RealTime;
        public void RealTimeToSource() => RealTime = Source;
        public void RealTimeToTarget() => RealTime = Target;
    }
}