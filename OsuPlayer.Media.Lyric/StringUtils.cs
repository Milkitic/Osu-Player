using System;

namespace Milky.OsuPlayer.Media.Lyric
{
    internal static class StringUtils
    {
        public static int GetEditDistance(string a, string b)
        {
            int lenA = a.Length;
            int lenB = b.Length;

            int[,] dp = new int[lenA + 1, lenB + 1];

            for (int i = 0; i <= lenA; i++)
                dp[i, 0] = i;

            for (int j = 0; j <= lenB; j++)
                dp[0, j] = j;

            for (int i = 0; i < lenA; i++)
            {
                char cA = a[i];
                for (int j = 0; j < lenB; j++)
                {
                    char cB = b[j];

                    if (cA == cB)
                    {
                        dp[i + 1, j + 1] = dp[i, j];
                    }
                    else
                    {
                        int replace = dp[i, j] + 1;
                        int insert = dp[i, j + 1] + 1;
                        int delete = dp[i + 1, j] + 1;

                        int min = Math.Min(Math.Min(insert, replace), delete);
                        dp[i + 1, j + 1] = min;
                    }
                }
            }

            return dp[lenA, lenB];
        }
    }
}
