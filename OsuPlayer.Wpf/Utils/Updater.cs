using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Utils
{
    public class Updater
    {
        public string CurrentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().TrimEnd('.', '0');
        private const int Timeout = 10000;
        private const int RetryCount = 3;
        private static readonly HttpClient HttpClient;
        public Release NewRelease { get; private set; }
        public bool IsRunningChecking;

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

                    Console.WriteLine(@"Get");
                    List<Release> releases = JsonConvert.DeserializeObject<List<Release>>(json);
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

                    if (latestVerObj <= nowVerObj)
                    {
                        NewRelease = null;
                        result = false;
                        return;
                    }

                    NewRelease = latest;
                    NewRelease.NewVerString = "v" + latestVer;
                    NewRelease.NowVerString = "v" + CurrentVersion;
                    NewRelease.Body = NewRelease.HtmlUrl + Environment.NewLine + NewRelease.Body;
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

        static Updater()
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
