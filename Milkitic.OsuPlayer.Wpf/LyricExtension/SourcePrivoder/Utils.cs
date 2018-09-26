using System;
using System.Text;

namespace Milkitic.OsuPlayer.Wpf.LyricExtension.SourcePrivoder
{
    internal static class Utils
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

        /// <summary>
        /// Base64加密，采用utf8编码方式加密
        /// </summary>
        /// <param name="source">待加密的明文</param>
        /// <returns>加密后的字符串</returns>
        public static string Base64Encode(string source)
        {
            return Base64Encode(Encoding.UTF8, source);
        }

        /// <summary>
        /// Base64加密
        /// </summary>
        /// <param name="encodeType">加密采用的编码方式</param>
        /// <param name="source">待加密的明文</param>
        /// <returns></returns>
        public static string Base64Encode(Encoding encodeType, string source)
        {
            string encode = string.Empty;
            byte[] bytes = encodeType.GetBytes(source);
            try
            {
                encode = Convert.ToBase64String(bytes);
            }
            catch
            {
                encode = source;
            }
            return encode;
        }

        /// <summary>
        /// Base64解密，采用utf8编码方式解密
        /// </summary>
        /// <param name="result">待解密的密文</param>
        /// <returns>解密后的字符串</returns>
        public static string Base64Decode(string result)
        {
            return Base64Decode(Encoding.UTF8, result);
        }

        /// <summary>
        /// Base64解密
        /// </summary>
        /// <param name="encodeType">解密采用的编码方式，注意和加密时采用的方式一致</param>
        /// <param name="result">待解密的密文</param>
        /// <returns>解密后的字符串</returns>
        public static string Base64Decode(Encoding encodeType, string result)
        {
            string decode = string.Empty;
            byte[] bytes = Convert.FromBase64String(result);
            try
            {
                decode = encodeType.GetString(bytes);
            }
            catch
            {
                decode = result;
            }
            return decode;
        }
    }
}
