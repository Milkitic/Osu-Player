namespace OsuPlayer.Iidx.Abstractions;

/// <summary>
/// Identifies the IIDX game version series a chart was authored for. Affects
/// tick-to-millisecond conversion (different <c>fps</c> per era).
/// </summary>
/// <remarks>
/// Ported from <c>IIDXToolbox.GameVersion</c> to avoid pulling the entire
/// toolbox project as a dependency.
/// </remarks>
public enum IidxGameVersion
{
    BeforeGold,
    Gold,
    AfterGold
}

public static class IidxGameVersionExtensions
{
    /// <summary>
    /// Converts a raw chart tick into milliseconds using the version-specific
    /// tick frequency. See <seealso href="https://github.com/SaxxonPike/rhythm-game-formats/blob/master/iidx/1.md"/>.
    /// </summary>
    public static int ConvertTickToOffsetInMilliseconds(this IidxGameVersion version, int tick)
    {
        return version switch
        {
            IidxGameVersion.BeforeGold => (int)(tick * 16.6833500166834D), // 1000 / 59.94
            IidxGameVersion.Gold => (int)(tick * 16.6538986776804D),       // 1000 / 60.046
            _ => tick
        };
    }
}