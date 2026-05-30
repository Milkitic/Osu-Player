using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Shared;

public sealed record DownloadProgress(long TotalBytes, long DownloadedBytes, long BytesPerSecond)
{
    public bool HasKnownTotal => TotalBytes > 0;
    public double Percentage => HasKnownTotal ? DownloadedBytes / (double)TotalBytes * 100 : 0;
}

public class GithubAssetsDownloader
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    private string Url { get; set; }
    private readonly CancellationTokenSource _cts = new();

    public bool IsCancellationRequested => _cts.IsCancellationRequested;

    public GithubAssetsDownloader(string url)
    {
        Url = url;
    }

    public async Task DownloadAsync(
        string savePath,
        IProgress<DownloadProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        var effectiveToken = linkedCts.Token;

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(30000);
            client.DefaultRequestHeaders.UseOsuPlayerUserAgent();

            using var response = await client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, effectiveToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var responseStream =
                await response.Content.ReadAsStreamAsync(effectiveToken).ConfigureAwait(false);
            var fileSize = response.Content.Headers.ContentLength ?? -1;
            progress?.Report(new DownloadProgress(fileSize, 0, 0));

            var dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var buffer = new byte[81920];
            long totalSize = 0;
            var stopwatch = Stopwatch.StartNew();

            await using var fs = new FileStream(
                savePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                buffer.Length,
                true);

            while (true)
            {
                var size = await responseStream.ReadAsync(buffer.AsMemory(0, buffer.Length), effectiveToken)
                    .ConfigureAwait(false);
                if (size == 0)
                    break;

                totalSize += size;
                await fs.WriteAsync(buffer.AsMemory(0, size), effectiveToken).ConfigureAwait(false);

                var elapsedSeconds = Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
                var speed = (long)(totalSize / elapsedSeconds);
                progress?.Report(new DownloadProgress(fileSize, totalSize, speed));
            }
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            s_logger.Error("Connection error. Target URI: {0}", Url);
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            s_logger.Error("Connection timed out. Target URI: {0}", Url);
            throw;
        }
    }

    public void Interrupt()
    {
        _cts.Cancel();
    }
}