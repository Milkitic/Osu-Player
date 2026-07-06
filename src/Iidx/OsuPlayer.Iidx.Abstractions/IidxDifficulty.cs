namespace OsuPlayer.Iidx.Abstractions;

/// <summary>
/// Beatmania IIDX difficulty slot. Order mirrors the IIDXToolbox
/// <c>ChartDifficulty</c> enum so chart file index mappings stay stable.
/// </summary>
public enum IidxDifficulty
{
    SpBeginner,
    SpNormal,
    SpHyper,
    SpAnother,
    SpLegendaria,
    DpBeginner,
    DpNormal,
    DpHyper,
    DpAnother,
    DpLegendaria
}

/// <summary>
/// Maps an <see cref="IidxDifficulty"/> to its canonical short label used by
/// IIDX data files (<c>SPB</c>, <c>SPN</c>, etc.). Useful for display and
/// for indexing into the per-difficulty file-identifier arrays of
/// <c>MusicDbEntry32</c>.
/// </summary>
public static class IidxDifficultyLabels
{
    private static readonly string[] s_labels =
    [
        "SPB", "SPN", "SPH", "SPA", "SPL",
        "DPB", "DPN", "DPH", "DPA", "DPL"
    ];

    public static string ShortLabel(IidxDifficulty difficulty) =>
        s_labels[(int)difficulty];

    public static IReadOnlyList<string> AllLabels => s_labels;
}