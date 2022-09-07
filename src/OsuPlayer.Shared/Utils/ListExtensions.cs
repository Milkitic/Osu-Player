namespace Milki.OsuPlayer.Shared.Utils;

public static class ListExtensions
{
    private static readonly Random Rnd = new Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Rnd.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}