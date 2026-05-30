using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Path = System.IO.Path;

namespace Milky.OsuPlayer
{
    /// <summary>
    /// UpdateWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UpdateWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly GithubRelease _release;
        private readonly MainWindow _mainWindow;
        private GithubAssetsDownloader _githubAssetsDownloader;
        private readonly string _savePath = Path.Combine(Domain.CurrentPath, "update.zip");

        public UpdateWindow(GithubRelease release, MainWindow mainWindow)
        {
            _release = release;
            _mainWindow = mainWindow;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var asset = _release?.Assets.FirstOrDefault(k => k.Name == "Osu-Player.zip");
            if (asset == null) return;
            _mainWindow.ForceClose();
            _githubAssetsDownloader = new GithubAssetsDownloader(asset.BrowserDownloadUrl);
            try
            {
                var progress = new Progress<DownloadProgress>(UpdateDownloadProgress);
                await _githubAssetsDownloader.DownloadAsync(_savePath, progress);
                OpenDownloadedUpdate();
            }
            catch (OperationCanceledException) when (_githubAssetsDownloader.IsCancellationRequested)
            {
                Logger.Info("Update download canceled.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while updating.");
                MessageBox.Show(this, "更新出错，请重启软件重试：" + ex.Message, Title,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDownloadProgress(DownloadProgress progress)
        {
            if (progress.HasKnownTotal)
            {
                DlProgress.IsIndeterminate = false;
                DlProgress.Maximum = progress.TotalBytes;
                DlProgress.Value = progress.DownloadedBytes;
                LblProgress.Content = $"{Math.Round(progress.Percentage)} %";
            }
            else
            {
                DlProgress.IsIndeterminate = true;
                LblProgress.Content = "-- %";
            }

            LblSpeed.Content = SharedUtils.CountSize(progress.BytesPerSecond) + "/s";
        }

        private void OpenDownloadedUpdate()
        {
            Process.Start(new FileInfo(_savePath).DirectoryName!);
            Process.Start(_savePath);
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _githubAssetsDownloader?.Interrupt();
        }
    }
}