using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OsuPlayer.Core.Configuration;

/// <summary>
/// Persisted DirectSound-style effect configuration. Backed by
/// <see cref="DirectXEffectSettings"/> at runtime — the two stay in
/// sync via <see cref="ToSettings"/> / <see cref="LoadFromSettings"/>.
/// </summary>
public partial class EffectsSection : ObservableObject
{
    [ObservableProperty]
    public partial DirectXEffectKind Kind { get; set; } = DirectXEffectKind.None;

    /// <summary>Master wet/dry send level in <c>[-1, +1]</c>.</summary>
    [ObservableProperty]
    public partial float Intensity { get; set; }

    [ObservableProperty]
    public partial bool ApplyToHitsound { get; set; } = true;

    [ObservableProperty]
    public partial bool ApplyToBackground { get; set; }

    [ObservableProperty]
    public partial bool ApplyToMusic { get; set; }

    /// <summary>
    /// Per-effect parameter sets. Persisted as a nested object so the
    /// defaults survive even when the user has not yet picked a
    /// particular effect in the UI.
    /// </summary>
    [ObservableProperty]
    public partial EffectParameterSet Parameters { get; set; } = new();

    public DirectXEffectSettings ToSettings() => new()
    {
        Kind = Kind,
        Intensity = Math.Clamp(Intensity, -1f, 1f),
        ApplyToHitsound = ApplyToHitsound,
        ApplyToBackground = ApplyToBackground,
        ApplyToMusic = ApplyToMusic,
        Parameters = Parameters.Clone(),
    };

    public void LoadFromSettings(DirectXEffectSettings settings)
    {
        Kind = settings.Kind;
        Intensity = Math.Clamp(settings.Intensity, -1f, 1f);
        ApplyToHitsound = settings.ApplyToHitsound;
        ApplyToBackground = settings.ApplyToBackground;
        ApplyToMusic = settings.ApplyToMusic;
    }

    /// <summary>
    /// Raises <see cref="ObservableObject.PropertyChanged"/> for the
    /// <see cref="Parameters"/> property. Call this after mutating any
    /// nested parameter object so audio engine subscribers can push the
    /// new values to the live effect chain.
    /// </summary>
    public void NotifyParametersChanged() => OnPropertyChanged(nameof(Parameters));
}
