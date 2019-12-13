namespace Milky.OsuPlayer.Media.Audio
{
    public interface ISoundElement
    {
        double Offset { get; }
        float Volume { get; }
        float Balance { get; }
        string[] FilePaths { get; }
    }
}