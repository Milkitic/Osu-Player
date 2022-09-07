namespace Milki.OsuPlayer.Audio.Internal;

internal static class MathEx
{
    public static T Max<T>(params T[] values) where T : struct, IComparable
    {
        return Max(values.AsEnumerable());
    }

    public static T Max<T>(IEnumerable<T> values) where T : struct, IComparable
    {
        var def = default(T);
        foreach (var value in values)
        {
            if (def.CompareTo(value) < 0) def = value;
        }

        return def;
    }
}