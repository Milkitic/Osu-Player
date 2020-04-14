using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Shared
{
    public class Downloader
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public delegate void StartDownloadingHandler(long size);
        public delegate void DownloadingHandler(long size, long downloadedSize, long speed);
        public delegate void FinishDownloadingHandler();

        public event StartDownloadingHandler OnStartDownloading;
        public event DownloadingHandler OnDownloading;
        public event FinishDownloadingHandler OnFinishDownloading;

        private string Url { get; set; }
        private Task _downloadTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public Downloader(string url)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            this.Url = url;
        }

        public async Task DownloadAsync(string savePath)
        {
            _downloadTask = new Task(async () =>
            {
                using (var ms = new MemoryStream())
                {
                    try
                    {
                        var request = WebRequest.Create(this.Url) as HttpWebRequest;
                        if (request == null)
                        {
                            // todo
                            throw new Exception();
                        }

                        request.Timeout = 30000;
                        request.UserAgent =
                            "ozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36";

                        var response = await request.GetResponseAsync().ConfigureAwait(false) as HttpWebResponse;
                        if (response == null)
                        {
                            // todo
                            throw new Exception();
                        }

                        using (var responseStream = response.GetResponseStream())
                        {
                            if (responseStream == null)
                            {
                                // todo
                                throw new Exception();
                            }

                            var fileSize = response.ContentLength;

                            OnStartDownloading?.Invoke(fileSize);

                            var bArr = new byte[1024];
                            var size = responseStream.Read(bArr, 0, bArr.Length);

                            long totalSize = 0;
                            long lastSize = 0;
                            long speed = 0;
                            _ = Task.Run(() =>
                            {
                                bool isFinished = false;
                                OnFinishDownloading += () => { isFinished = true; };
                                const int interval = 500;
                                const int ratio = 1000 / interval;

                                while (!isFinished && !_cts.IsCancellationRequested)
                                {
                                    speed = (totalSize - lastSize) * ratio;
                                    lastSize = totalSize;
                                    Thread.Sleep(interval);
                                }
                            });

                            while (size > 0)
                            {
                                if (_cts.IsCancellationRequested)
                                {
                                    Logger.Info(@"Download canceled.");
                                    return;
                                }

                                totalSize += size;
                                ms.Write(bArr, 0, size);
                                size = responseStream.Read(bArr, 0, (int)bArr.Length);
                                OnDownloading?.Invoke(fileSize, totalSize, speed);
                            }

                            var dir = new FileInfo(savePath).Directory;
                            if (!dir.Exists)
                                dir.Create();
                            using (var fs = new FileStream(savePath, FileMode.Append, FileAccess.Write,
                                FileShare.ReadWrite))
                            {
                                fs.Write(ms.GetBuffer(), 0, (int)ms.Length);
                            }

                            OnFinishDownloading?.Invoke();
                        }
                    }
                    catch (WebException ex)
                    {
                        if (ex.InnerException is SocketException)
                        {
                            Logger.Error(@"Connection error. Target URI: {0}", Url);
                            throw;
                        }

                        if (ex.Status == WebExceptionStatus.Timeout)
                        {
                            Logger.Error(@"Connection timed out. Target URI: {0}", Url);
                            throw;
                        }

                        throw;
                    }
                }
            });
            _downloadTask.Start();
            await Task.WhenAll(_downloadTask).ConfigureAwait(false);
        }

        public void Interrupt()
        {
            _cts.Cancel();
            Task.WaitAll(_downloadTask);
        }
    }
}