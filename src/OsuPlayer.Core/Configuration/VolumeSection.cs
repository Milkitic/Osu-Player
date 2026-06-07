using CommunityToolkit.Mvvm.ComponentModel;

namespace OsuPlayer.Core.Configuration;

public class VolumeSection : ObservableObject
{
    public float Main
    {
        get;
        set => SetProperty(ref field, Clamp(value));
    } = 0.8f;

    public float Music
    {
        get;
        set => SetProperty(ref field, Clamp(value));
    } = 1;

    public float Hitsound
    {
        get;
        set => SetProperty(ref field, Clamp(value));
    } = 0.9f;

    public float Sample
    {
        get;
        set => SetProperty(ref field, Clamp(value));
    } = 0.85f;

    public float BalanceFactor
    {
        get => field * 100;
        set => SetProperty(ref field, Clamp(value / 100f));
    } = 0.35f;

    private static float Clamp(float value) => value < 0 ? 0 : (value > 1 ? 1 : value);
}
