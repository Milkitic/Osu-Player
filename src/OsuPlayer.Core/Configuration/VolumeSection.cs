using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OsuPlayer.Core.Configuration;

public enum BalanceModeSetting
{
    [Description("关闭")]
    Off,
    [Description("等幂声像")]
    ConstantPower,
    [Description("交叉混合")]
    CrossMix,
    [Description("Mid-Side")]
    MidSide,
    [Description("单声道混合")]
    BinauralMix,
}

public enum LimiterTypeSetting
{
    [Description("关闭")]
    Off,
    [Description("多项式")]
    Polynomial,
    [Description("主控")]
    Master,
    [Description("软限")]
    Soft,
    [Description("硬限")]
    Hard,
}

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

    public BalanceModeSetting BalanceMode
    {
        get;
        set => SetProperty(ref field, value);
    } = BalanceModeSetting.MidSide;

    public LimiterTypeSetting LimiterType
    {
        get;
        set => SetProperty(ref field, value);
    } = LimiterTypeSetting.Off;

    private static float Clamp(float value) => value < 0 ? 0 : (value > 1 ? 1 : value);
}
