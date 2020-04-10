using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Common.Instances
{
    public class UpdateInst
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private const int Timeout = 10000;
        private const int RetryCount = 3;
        private static readonly HttpClient HttpClient;

        public GithubRelease NewRelease { get; private set; }
        public bool IsRunningChecking { get; private set; }
        public string CurrentVersion { get; } = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString().TrimEnd('.', '0');

        public async Task<bool?> CheckUpdateAsync()
        {
            IsRunningChecking = true;

            try
            {
                string json = "";
                while (json == "")
                {
                    json = await HttpGetAsync("http://api.github.com/repos/Milkitic/Osu-Player/releases");
                }

                List<GithubRelease> releases = JsonConvert.DeserializeObject<List<GithubRelease>>(json);
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

                Logger.Info("Current version: {nowVer}; Got version info: {latestVer}", nowVerObj, latestVerObj);

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
                Logger.Error(ex, "Error while checking for updates.");
                throw;
            }

            IsRunningChecking = false;
            return true;
        }

        static UpdateInst()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpClient =
                new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
                {
                    Timeout = new TimeSpan(0, 0, 0, 0, Timeout)
                };
            HttpClient.DefaultRequestHeaders.Add("User-Agent",
                "ozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36");
        }

        private static async Task<string> HttpGetAsync(string url)
        {
            for (int i = 0; i < RetryCount; i++)
            {
                try
                {
                    var message = new HttpRequestMessage(HttpMethod.Get, url);
                    CancellationTokenSource cts = new CancellationTokenSource(Timeout);
                    var response = await HttpClient.SendAsync(message, cts.Token).ConfigureAwait(false);
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
