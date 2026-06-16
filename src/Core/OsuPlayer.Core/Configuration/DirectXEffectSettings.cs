using System;

namespace OsuPlayer.Core.Configuration;

/// <summary>
/// Immutable snapshot of the user-facing effect configuration. A new
/// instance is allocated whenever the UI publishes a change; consumers
/// apply it to the live effect chain in
/// <c>OsuPlayer.Media.Audio</c>.
/// </summary>
public sealed class DirectXEffectSettings : IEquatable<DirectXEffectSettings>
{
    /// <summary>
    /// Identifies the active effect. <see cref="DirectXEffectKind.None"/>
    /// means the chain is in pass-through mode.
    /// </summary>
    public DirectXEffectKind Kind { get; init; } = DirectXEffectKind.None;

    /// <summary>
    /// Master intensity in <c>[-1, +1]</c>. Negative values mean "lighter /
    /// drier" and positive values mean "heavier / wetter". A value at or
    /// below <c>-1</c> is treated as bypass.
    /// </summary>
    public float Intensity { get; init; }

    /// <summary>
    /// Apply the effect to the hitsound bus (whistles, finishes, claps).
    /// </summary>
    public bool ApplyToHitsound { get; init; } = true;

    /// <summary>
    /// Apply the effect to the background sample bus (storyboard samples
    /// and loops).
    /// </summary>
    public bool ApplyToBackground { get; init; }

    /// <summary>
    /// Apply the effect to the music bus.
    /// </summary>
    public bool ApplyToMusic { get; init; }

    /// <summary>
    /// Per-effect detailed parameters. Carried along with the snapshot so
    /// the audio engine can push them to the live effect chain.
    /// </summary>
    public EffectParameterSet Parameters { get; init; } = new();

    public static DirectXEffectSettings Disabled { get; } = new();

    public bool IsEffectActive => Kind != DirectXEffectKind.None && Intensity > -1f + 0.001f;

    public bool Equals(DirectXEffectSettings other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Kind == other.Kind
               && Intensity.Equals(other.Intensity)
               && ApplyToHitsound == other.ApplyToHitsound
               && ApplyToBackground == other.ApplyToBackground
               && ApplyToMusic == other.ApplyToMusic;
    }

    public override bool Equals(object obj) => Equals(obj as DirectXEffectSettings);

    public override int GetHashCode() => HashCode.Combine(Kind, Intensity, ApplyToHitsound, ApplyToBackground, ApplyToMusic);
}
