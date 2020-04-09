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
            bool? result = null;
            await Task.Run(() =>
            {
                try
                {
                    string json = "";
                    while (json == "")
                    {
                        json = HttpGet("http://api.github.com/repos/Milkitic/Osu-Player/releases");
                    }

                    List<GithubRelease> releases = JsonConvert.DeserializeObject<List<GithubRelease>>(json);
                    var latest = releases.OrderByDescending(k => k.PublishedAt)
                        .FirstOrDefault(k => !k.Draft && !k.PreRelease);
                    if (latest == null)
                    {
                        NewRelease = null;
                        result = false;
                        return;
                    }

                    var latestVer = latest.TagName.TrimStart('v').TrimEnd('.', '0');

                    Version latestVerObj = new Version(latestVer);
                    Version nowVerObj = new Version(CurrentVersion);

                    Logger.Info("Current version: {nowVer}", nowVerObj);
                    Logger.Info("Got version info: {latestVer}", latestVerObj);

                    if (latestVerObj <= nowVerObj)
                    {
                        NewRelease = null;
                        result = false;
                        return;
                    }

                    NewRelease = latest;
                    NewRelease.NewVerString = "v" + latestVer;
                    NewRelease.NowVerString = "v" + CurrentVersion;
                    result = true;
                }
                catch
                {
                    result = null;
                }
            });
            IsRunningChecking = false;
            return result;
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

        private static string HttpGet(string url)
        {
            for (int i = 0; i < RetryCount; i++)
            {
                try
                {
                    var message = new HttpRequestMessage(HttpMethod.Get, url);
                    CancellationTokenSource cts = new CancellationTokenSource(Timeout);
                    HttpResponseMessage response = HttpClient.SendAsync(message, cts.Token).Result;
                    return response.Content.ReadAsStringAsync().Result;
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
