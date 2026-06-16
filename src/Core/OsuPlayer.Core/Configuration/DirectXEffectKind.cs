using System.ComponentModel;

namespace OsuPlayer.Core.Configuration;

/// <summary>
/// Enumerates the DirectSound-style effects the player can host. The
/// <see cref="None"/> sentinel means "no effect" — the effect chain in
/// the audio module becomes a pass-through. The enum lives in
/// <c>OsuPlayer.Core</c> so the settings layer can reference it
/// without depending on the audio module (and vice versa).
/// </summary>
public enum DirectXEffectKind
{
    [Description("关闭")]
    None = 0,

    [Description("Compressor 压缩")]
    Compressor = 1,

    [Description("Chorus 合唱")]
    Chorus = 2,

    [Description("Gargle 颤音")]
    Gargle = 3,

    [Description("Reverb Ex 大混响")]
    ReverbEx = 4,

    [Description("Flanger 镶边")]
    Flanger = 5,

    [Description("Distortion 失真")]
    Distortion = 6,
}
