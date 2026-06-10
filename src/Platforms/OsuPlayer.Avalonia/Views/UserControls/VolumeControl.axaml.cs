using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Playback;
using OsuPlayer.Playback.Playlist;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Views.UserControls;

public partial class VolumeControlVm : ObservableObject
{
    public SharedVm Shared { get; } = SharedVm.Default;
    public BalanceModeSetting[] AvailableBalanceModes { get; } = Enum.GetValues<BalanceModeSetting>();
    public LimiterTypeSetting[] AvailableLimiterTypes { get; } = Enum.GetValues<LimiterTypeSetting>();
}

public partial class VolumeControl : UserControl
{
    private readonly IPlayerDataService? _playerData;
    private readonly ObservablePlayController? _controller;

    public VolumeControl()
    {
        if (App.Services != null)
        {
            _playerData = App.Services.GetService<IPlayerDataService>();
            _controller = App.Services.GetService<ObservablePlayController>();
        }

        InitializeComponent();

        MasterVolume.AddHandler(Slider.PointerReleasedEvent, (_, _) => AppSettings.SaveDefault());
        MusicVolume.AddHandler(Slider.PointerReleasedEvent, (_, _) => AppSettings.SaveDefault());
        HitsoundVolume.AddHandler(Slider.PointerReleasedEvent, (_, _) => AppSettings.SaveDefault());
        SampleVolume.AddHandler(Slider.PointerReleasedEvent, (_, _) => AppSettings.SaveDefault());
        Balance.AddHandler(Slider.PointerReleasedEvent, (_, _) => AppSettings.SaveDefault());

        Offset.AddHandler(Slider.PointerPressedEvent, (_, _) => OnOffsetDragDelta());
        Offset.AddHandler(Slider.PointerReleasedEvent, (_, _) => OnOffsetDragComplete());
    }

    private void OnLoaded(RoutedEventArgs e)
    {
        if (_controller == null) return;
        Offset.Value = _controller.PlayList.CurrentInfo?.BeatmapSettings?.Offset ?? 0;
        _controller.LoadFinished += Controller_LoadFinished;
    }

    private void Controller_LoadFinished(BeatmapContext bc, System.Threading.CancellationToken arg2)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Offset.Value = bc.BeatmapSettings.Offset);
    }

    private void OnOffsetDragDelta()
    {
        if (_controller?.Player == null) return;
        _controller.Player.ManualOffset = (int)Offset.Value;
    }

    private async void OnOffsetDragComplete()
    {
        if (_controller?.PlayList.CurrentInfo == null || _playerData == null) return;
        await _playerData.TryUpdateMapAsync(_controller.PlayList.CurrentInfo.Beatmap, _controller.Player.ManualOffset);
    }

    private async void BtnPlayMod_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_controller?.Player != null && sender is Button btn && btn.Tag is PlayModifier mod)
        {
            await _controller.Player.SetPlayMod(mod);
        }
    }
}
