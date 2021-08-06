using System;

namespace Milky.OsuPlayer.Media.Lyric.Models
{
    public struct Sentence : IEquatable<Sentence>, IComparable<Sentence>
    {
        public static Sentence Empty { get; private set; } = new Sentence(string.Empty, -1);

        public int StartTime { get; set; }
        public string Content { get; set; }

        public Sentence(string content, int start_time)
        {
            this.StartTime = start_time;
            this.Content = content;
        }

        public bool Equals(Sentence other)
        {
            return StartTime == other.StartTime && other.Content == Content;
        }

        public int CompareTo(Sentence other)
        {
            return StartTime - other.StartTime;
        }

        //1:07:224 67224
        public static string GetTimeline(int time)
        {
            int min = time / 60000;
            int sec = (time % 60000) / 1000;
            int msec = time - min * 60000 - sec * 1000;

            return $"[{min}:{sec}:{msec}]";
        }

        public override string ToString()
        {
            return $"{GetTimeline(StartTime)}{Content}";
        }

        public static Sentence operator +(Sentence a, Sentence b)
        {
            return new Sentence(a.Content + Environment.NewLine + b.Content, ((a.StartTime < 0 ? b.StartTime : a.StartTime) + (b.StartTime < 0 ? a.StartTime : b.StartTime)) / 2);
        }
    }
}
