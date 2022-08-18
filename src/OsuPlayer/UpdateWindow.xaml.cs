﻿using System.Diagnostics;
using System.IO;
using System.Windows;
using Anotar.NLog;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer;

/// <summary>
/// UpdateWindow.xaml 的交互逻辑
/// </summary>
public partial class UpdateWindow : Window
{
    private readonly GithubRelease _release;
    private readonly MainWindow _mainWindow;
    private Downloader _downloader;
    private readonly string _savePath = Path.Combine(Environment.CurrentDirectory, "update.zip");

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
        _downloader = new Downloader(asset.BrowserDownloadUrl);
        _downloader.OnStartDownloading += Downloader_OnStartDownloading;
        _downloader.OnDownloading += Downloader_OnDownloading;
        _downloader.OnFinishDownloading += Downloader_OnFinishDownloading;
        try
        {
            await _downloader.DownloadAsync(_savePath);
        }
        catch (Exception ex)
        {
            LogTo.Error("Error while updating.", ex);
            MessageBox.Show(this, "更新出错，请重启软件重试：" + ex.Message, Title,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Downloader_OnStartDownloading(long size)
    {
        Dispatcher.BeginInvoke(new Action(() => DlProgress.Maximum = size));
    }

    private void Downloader_OnDownloading(long size, long downloadedSize, long speed)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            DlProgress.Value = downloadedSize;
            LblSpeed.Content = SharedUtils.CountSize(speed) + "/s";
            LblProgress.Content = $"{Math.Round(downloadedSize / (float)size * 100)} %";
        }));
    }

    private void Downloader_OnFinishDownloading()
    {
        Process.Start(new FileInfo(_savePath).DirectoryName);
        Process.Start(_savePath);
        Dispatcher.BeginInvoke(new Action(Close));
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _downloader.Interrupt();
    }
}