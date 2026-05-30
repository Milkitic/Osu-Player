using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Shared
{
    public class UpdateInst
    {
        private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly HttpClient s_httpClient;

        private const int Timeout = 10000;
        private const int RetryCount = 3;

        public GithubRelease NewRelease { get; private set; }
        public bool IsRunningChecking { get; private set; }
        public string CurrentVersion { get; } = Assembly.GetEntryAssembly().GetName().Version.ToString().TrimEnd('.', '0');

        public async Task<bool?> CheckUpdateAsync()
        {
            IsRunningChecking = true;

            try
            {
                var json = "";
                while (json == "") json = await HttpGetAsync("http://api.github.com/repos/Milkitic/Osu-Player/releases");

                if (json.Contains("API rate limit"))
                {
                    s_logger.Error("Error while checking for updates: API rate limit");
                    throw new Exception("Github API rate limit exceeded.");
                }

                var releases = JsonSerializer.Deserialize<List<GithubRelease>>(json);
                var latest = releases.OrderByDescending(k => k.PublishedAt)
                    .FirstOrDefault(k => !k.Draft && !k.PreRelease);
                if (latest == null)
                {
                    NewRelease = null;
                    return false;
                }

                var latestVer = latest.TagName.TrimStart('v').TrimEnd('.', '0');

                var latestVerObj = new Version(latestVer);
                var nowVerObj = new Version(CurrentVersion);

                s_logger.Info("Current version: {nowVer}; Got version info: {latestVer}", nowVerObj, latestVerObj);

                if (latestVerObj <= nowVerObj)
                {
                    NewRelease = null;
                    return false;
                }

                NewRelease = latest;
                NewRelease.NewVerString = "v" + latestVer;
                NewRelease.NowVerString = "v" + CurrentVersion;
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "Error while checking for updates.");
                throw;
            }

            IsRunningChecking = false;
            return true;
        }

        static UpdateInst()
        {
            s_httpClient =
                new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
                {
                    Timeout = new TimeSpan(0, 0, 0, 0, Timeout)
                };
            s_httpClient.DefaultRequestHeaders.UseOsuPlayerUserAgent();
        }

        private static async Task<string> HttpGetAsync(string url)
        {
            for (var i = 0; i < RetryCount; i++)
            {
                try
                {
                    var message = new HttpRequestMessage(HttpMethod.Get, url);
                    var cts = new CancellationTokenSource(Timeout);
                    var response = await s_httpClient.SendAsync(message, cts.Token).ConfigureAwait(false);
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    if (i == RetryCount - 1)
                        throw;
                }
            }

            return null;
        }
    }
}
