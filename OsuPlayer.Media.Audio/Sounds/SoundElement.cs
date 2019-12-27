namespace Milky.OsuPlayer.Media.Audio.Sounds
{
    public abstract class SoundElement
    {
        public abstract double Offset { get; protected set; }
        public abstract float Volume { get; protected set; }
        public abstract float Balance { get; protected set; }
        public abstract string[] FilePaths { get; protected set; }

        public const string WavExtension = ".wav";
        public const string OggExtension = ".ogg";
    }
}