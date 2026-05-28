using System;
using System.Net.Http.Headers;
using System.Reflection;

namespace Milky.OsuPlayer.Shared;

public static class HttpRequestHeaderUtilities
{
    private const string ProductName = "Osu-Player";
    private const string RepositoryUrl = "https://github.com/Milkitic/Osu-Player";

    public static void UseOsuPlayerUserAgent(this HttpRequestHeaders headers)
    {
        headers.UserAgent.Clear();
        headers.UserAgent.Add(new ProductInfoHeaderValue(ProductName, GetProductVersion()));
        headers.UserAgent.Add(new ProductInfoHeaderValue($"(+{RepositoryUrl})"));
    }

    private static string GetProductVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version
                      ?? typeof(HttpRequestHeaderUtilities).Assembly.GetName().Version;

        if (version == null)
            return "1.0";

        return version.Build >= 0 ? version.ToString(3) : version.ToString(2);
    }
}