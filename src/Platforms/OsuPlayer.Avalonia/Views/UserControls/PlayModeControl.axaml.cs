using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Controls;
using OsuPlayer.Playback;
using OsuPlayer.Playback.Playlist;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Views.UserControls;

public partial class PlayModeControl : UserControl
{
    public event EventHandler? CloseRequested;

    private readonly ObservablePlayController? _controller;

    public PlayModeControl()
    {
        if (App.Services != null)
        {
            _controller = App.Services.GetService<ObservablePlayController>();
        }

        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (_controller == null) return;
        _controller.PlayList.PropertyChanged += Player_PropertyChanged;
        SwitchOption(_controller.PlayList.Mode);
    }

    private void Player_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayList.Mode))
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => SwitchOption(_controller!.PlayList.Mode));
        }
    }

    private void SwitchOption(PlaylistMode playMode)
    {
        switch (playMode)
        {
            case PlaylistMode.Normal:
                ModeNormal.IsChecked = true;
                break;
            case PlaylistMode.Random:
                ModeRandom.IsChecked = true;
                break;
            case PlaylistMode.Loop:
                ModeLoop.IsChecked = true;
                break;
            case PlaylistMode.LoopRandom:
                ModeLoopRandom.IsChecked = true;
                break;
            case PlaylistMode.Single:
                ModeSingle.IsChecked = true;
                break;
            case PlaylistMode.SingleLoop:
                ModeSingleLoop.IsChecked = true;
                break;
        }
    }

    private void Mode_Changed(object? sender, RoutedEventArgs e)
    {
        if (sender is IconRadioButton radio && radio.IsChecked == true && radio.Tag is PlaylistMode mode)
        {
            _controller!.PlayList.Mode = mode;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
