#nullable enable

using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using Anotar.NLog;
using Milki.OsuPlayer.Shared.Models;
using Semver;

namespace Milki.OsuPlayer.Services;

public class UpdateService
{
    private const string Repo = "Milkitic/Osu-Player";
    private static string? _version;
    private const int Timeout = 10000;
    private const int RetryCount = 3;
    private readonly HttpClient _httpClient;

    public UpdateService()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        _httpClient =
            new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
            {
                Timeout = new TimeSpan(0, 0, 0, 0, Timeout)
            };
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "ozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36");
    }

    public GithubRelease? NewRelease { get; private set; }
    public bool IsRunningChecking { get; private set; }

    public string GetVersion()
    {
        if (_version != null) return _version;

        var assembly = Assembly.GetEntryAssembly();
        var version = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        _version = version ?? "";
        return _version;
    }

    public async Task<bool?> CheckUpdateAsync()
    {
        IsRunningChecking = true;

        try
        {
            string? json = "";
            while (json == "")
            {
                json = await HttpGetAsync($"https://api.github.com/repos/{Repo}/releases");
            }

            if (json == null) return null;
            if (json.Contains("API rate limit"))
            {
                LogTo.Error("Error while checking for updates: Github API rate limit exceeded.");
                return null;
            }

            var releases = JsonSerializer.Deserialize<List<GithubRelease>>(json)!;
            var latest = releases
                .OrderByDescending(k => k.PublishedAt)
                .FirstOrDefault(k => !k.Draft /*&& !k.PreRelease*/);
            if (latest == null)
            {
                NewRelease = null;
                return false;
            }

            var latestVer = latest.TagName.TrimStart('v');

            var latestVerObj = SemVersion.Parse(latestVer, SemVersionStyles.Strict);
            var nowVerObj = SemVersion.Parse(GetVersion(), SemVersionStyles.Strict);

            LogTo.Debug($"Current version: {nowVerObj}; Got version info: {latestVerObj}");

            if (latestVerObj.ComparePrecedenceTo(nowVerObj) <= 0)
            {
                NewRelease = null;
                return false;
            }

            NewRelease = latest;
            NewRelease.NewVerString = latestVer;
            NewRelease.NowVerString = GetVersion();
        }
        catch (Exception ex)
        {
            LogTo.Error($"Error while checking for updates: {ex.Message}");
            return null;
            //throw;
        }

        IsRunningChecking = false;
        return true;
    }

    private async Task<string?> HttpGetAsync(string url)
    {
        for (int i = 0; i < RetryCount; i++)
        {
            try
            {
                var message = new HttpRequestMessage(HttpMethod.Get, url);
                using var cts = new CancellationTokenSource(Timeout);
                var response = await _httpClient.SendAsync(message, cts.Token).ConfigureAwait(false);
                return await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
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