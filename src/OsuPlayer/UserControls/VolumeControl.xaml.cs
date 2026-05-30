using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Core.Configuration;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Services;

namespace Milky.OsuPlayer.UserControls;

public partial class VolumeControlVm : ObservableObject
{
    public SharedVm Shared { get; } = SharedVm.Default;
}

/// <summary>
/// VolumeControl.xaml 的交互逻辑
/// </summary>
public partial class VolumeControl : UserControl
{
    private readonly IPlayerDataService _playerData;
    private readonly ObservablePlayController _controller;

    public VolumeControl()
    {
        if (App.Services != null)
        {
            _playerData = App.Services.GetRequiredService<IPlayerDataService>();
            _controller = App.Services.GetRequiredService<ObservablePlayController>();
        }

        InitializeComponent();
    }

    private void VolumeControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_controller != null)
        {
            Offset.Value = _controller.PlayList.CurrentInfo?.BeatmapSettings?.Offset ?? 0;
            _controller.LoadFinished += Controller_LoadFinished;
        }
    }

    private void Controller_LoadFinished(BeatmapContext bc, System.Threading.CancellationToken arg2)
    {
        Offset.Value = bc.BeatmapSettings.Offset;
    }

    private void MasterVolume_DragComplete(object sender, DragCompletedEventArgs e)
    {
        AppSettings.SaveDefault();
    }

    private void MusicVolume_DragComplete(object sender, DragCompletedEventArgs e)
    {
        AppSettings.SaveDefault();
    }

    private void HitsoundVolume_DragComplete(object sender, DragCompletedEventArgs e)
    {
        AppSettings.SaveDefault();
    }

    private void SampleVolume_DragComplete(object sender, DragCompletedEventArgs e)
    {
        AppSettings.SaveDefault();
    }

    private void Balance_DragComplete(object sender, DragCompletedEventArgs e)
    {
        AppSettings.SaveDefault();
    }

    private void Offset_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_controller.Player == null)
            return;
        _controller.Player.ManualOffset = (int)Offset.Value;
    }

    private async void Offset_DragComplete(object sender, DragCompletedEventArgs e)
    {
        if (_controller.PlayList.CurrentInfo == null) return;
        await _playerData.TryUpdateMapAsync(_controller.PlayList.CurrentInfo.Beatmap,
            _controller.Player.ManualOffset);
    }

    private async void BtnPlayMod_OnClick(object sender, RoutedEventArgs e)
    {
        if (_controller.Player != null)
            await _controller.Player.SetPlayMod((PlayModifier)((Button)sender).Tag);
    }
}