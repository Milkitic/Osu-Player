using System;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Media.Audio;
using OsuPlayer.Playback;
using OsuPlayer.Playback.Playlist;
using OsuPlayer.Shared;

namespace OsuPlayer.Views.UserControls;

public partial class AnimationControlVm : ObservableObject
{
    [ObservableProperty]
    public partial Bitmap? BackgroundSource { get; set; }
}

public partial class AnimationControl : UserControl
{
    private readonly AnimationControlVm _viewModel = new();
    private readonly ObservablePlayController? _controller;
    private string? _backgroundPath;

    public AnimationControl()
    {
        InitializeComponent();
        DataContext = _viewModel;

        if (App.Services != null)
        {
            _controller = App.Services.GetService<ObservablePlayController>();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        LoadDefaultBackground();

        if (_controller == null) return;
        _controller.LoadStarted += Controller_LoadStarted;
        _controller.BackgroundInfoLoaded += Controller_BackgroundInfoLoaded;
        _controller.InterfaceClearRequest += Controller_InterfaceClearRequest;
        _controller.LoadError += Controller_LoadError;

        var currentBackground = _controller.PlayList.CurrentInfo?.BeatmapDetail.BackgroundPath;
        if (!string.IsNullOrWhiteSpace(currentBackground))
        {
            SetBackground(currentBackground);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_controller != null)
        {
            _controller.LoadStarted -= Controller_LoadStarted;
            _controller.BackgroundInfoLoaded -= Controller_BackgroundInfoLoaded;
            _controller.InterfaceClearRequest -= Controller_InterfaceClearRequest;
            _controller.LoadError -= Controller_LoadError;
        }

        ClearBackground();
        base.OnDetachedFromVisualTree(e);
    }

    private void Controller_LoadStarted(BeatmapContext beatmapCtx, CancellationToken ct)
        => Dispatcher.UIThread.Post(LoadDefaultBackground);

    private void Controller_BackgroundInfoLoaded(BeatmapContext beatmapCtx, CancellationToken ct)
        => Dispatcher.UIThread.Post(() => SetBackground(beatmapCtx.BeatmapDetail.BackgroundPath));

    private void Controller_InterfaceClearRequest()
        => Dispatcher.UIThread.Post(LoadDefaultBackground);

    private void Controller_LoadError(BeatmapContext beatmapCtx, Exception ex)
        => Dispatcher.UIThread.Post(LoadDefaultBackground);

    private void LoadDefaultBackground()
    {
        var path = Path.Combine(AppPaths.Current.ResourcePath, "official", "registration.jpg");
        SetBackground(path);
    }

    private void SetBackground(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            ClearBackground();
            return;
        }

        if (_viewModel.BackgroundSource != null &&
            string.Equals(_backgroundPath, path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            var bitmap = new Bitmap(path);
            var old = _viewModel.BackgroundSource;
            _backgroundPath = path;
            _viewModel.BackgroundSource = bitmap;
            old?.Dispose();
        }
        catch
        {
            ClearBackground();
        }
    }

    private void ClearBackground()
    {
        var old = _viewModel.BackgroundSource;
        _backgroundPath = null;
        _viewModel.BackgroundSource = null;
        old?.Dispose();
    }
}
