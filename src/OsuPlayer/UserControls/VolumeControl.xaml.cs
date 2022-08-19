using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.UserControls;

public class VolumeControlVm : VmBase
{
    public SharedVm Shared => SharedVm.Default;
}

/// <summary>
/// VolumeControl.xaml 的交互逻辑
/// </summary>
public partial class VolumeControl : UserControl
{
    private readonly PlayerService _playerService;

    public VolumeControl()
    {
        _playerService = ServiceProviders.Default.GetService<PlayerService>()!;
        _playerService.LoadFinished += PlayerService_LoadFinished;
        InitializeComponent();
    }

    private ValueTask PlayerService_LoadFinished(PlayerService.PlayItemLoadContext arg)
    {
        Offset.Value = arg.PlayItem?.PlayItemConfig?.Offset ?? 0;
        return ValueTask.CompletedTask;
    }

    private void VolumeControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        Offset.Value = _playerService.LastLoadContext?.PlayItem?.PlayItemConfig?.Offset ?? 0;
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
        if (_playerService.ActiveMixPlayer == null)
        {
            return;
        }

        _playerService.ActiveMixPlayer.Offset = (int)Offset.Value;
    }

    private async void Offset_DragComplete(object sender, DragCompletedEventArgs e)
    {
        if (_playerService.LastLoadContext?.PlayItem?.PlayItemConfig == null) return;
        await using var dbContext = new ApplicationDbContext();
        _playerService.LastLoadContext.PlayItem.PlayItemConfig.Offset =
            (int)(_playerService.ActiveMixPlayer?.Offset ?? 0d);

        await dbContext.UpdateAndSaveChangesAsync(_playerService.LastLoadContext.PlayItem.PlayItemConfig,
            k => k.Offset);
    }

    private void BtnPlayMod_OnClick(object sender, RoutedEventArgs e)
    {
        if (_playerService.ActiveMixPlayer != null)
        {
            _playerService.ActiveMixPlayer.PlayModifier = (PlayModifier)((Button)sender).Tag;
        }
    }
}