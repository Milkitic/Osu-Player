namespace Milky.OsuPlayer.Media.Audio.SoundTouch;

internal sealed class SoundTouchRateOptions
{
    public SoundTouchRateOptions(bool preservePitch, bool useAntiAliasing, bool useQuickSeek)
    {
        PreservePitch = preservePitch;
        UseAntiAliasing = useAntiAliasing;
        UseQuickSeek = useQuickSeek;
    }

    public bool PreservePitch { get; }

    public bool UseAntiAliasing { get; }

    public bool UseQuickSeek { get; }
}
