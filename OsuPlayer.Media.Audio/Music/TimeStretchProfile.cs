namespace Milky.OsuPlayer.Media.Audio.Music
{
    public class TimeStretchProfile
    {
        public string Id { get; set; }
        public string Description { get; set; }

        public bool UseAAFilter { get; set; }
        public int AAFilterLength { get; set; }
        public int Overlap { get; set; }
        public int Sequence { get; set; }
        public int SeekWindow { get; set; }

        public override string ToString()
        {
            return Description;
        }
    }
}
