namespace Milky.OsuPlayer.Media.Audio.Sounds
{
    public sealed class SpecificFileSoundElement : SoundElement
    {
        public SpecificFileSoundElement(float volume, float balance, string filePath, double offset)
        {
            Volume = volume;
            Balance = balance;
            FilePaths = new[] { filePath };
            Offset = offset;
        }

        public override float Volume { get; protected set; }
        public override float Balance { get; protected set; }
        public override string[] FilePaths { get; protected set; }
        public override double Offset { get; protected set; }
    }
}