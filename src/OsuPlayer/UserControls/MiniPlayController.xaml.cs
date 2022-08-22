using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.UserControls;

public class MiniPlayListControlVm : VmBase
{
    private double _positionPercent;

    public SharedVm Shared { get; } = SharedVm.Default;

    //public ICommand PlayPrevCommand => new DelegateCommand(async param => await _controller.PlayPrevAsync());

    //public ICommand PlayNextCommand => new DelegateCommand(async param => await _controller.PlayNextAsync());

    //public ICommand PlayPauseCommand => new DelegateCommand(param => _controller.Player.TogglePlay());

    public double PositionPercent
    {
        get => _positionPercent;
        set => this.RaiseAndSetIfChanged(ref _positionPercent, value);
    }
}

/// <summary>
/// MiniPlayController.xaml 的交互逻辑
/// </summary>
public partial class MiniPlayController : UserControl
{
    private readonly MiniPlayListControlVm _viewModel;
    private readonly PlayerService _controller;

    public static event Action MaxButtonClicked;
    public static event Action CloseButtonClicked;

    public MiniPlayController()
    {
        _controller = ServiceProviders.Default.GetService<PlayerService>();
        DataContext = _viewModel = new MiniPlayListControlVm();
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        PlayModeControl.CloseRequested += (obj, args) => PopMode.IsOpen = false;
        if (_controller != null)
        {
            _controller.PlayTimeChanged += AudioPlayer_PlayTimeChanged;
        }
    }

    private void AudioPlayer_PlayTimeChanged(TimeSpan time)
    {
        _viewModel.PositionPercent = time.TotalMilliseconds / _controller.TotalTime.TotalMilliseconds;
    }

    private void MaxButton_Click(object sender, RoutedEventArgs e)
    {
        MaxButtonClicked?.Invoke();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseButtonClicked?.Invoke();
    }

    private void VolumeButton_Click(object sender, RoutedEventArgs e)
    {
        PopVolume.IsOpen = true;
    }

    private void PlayListControl_CloseRequested(object sender, RoutedEventArgs e)
    {
        PopPlayList.IsOpen = false;
    }

    private void PlayListButton_Click(object sender, RoutedEventArgs e)
    {
        PopPlayList.IsOpen = true;
    }

    private void ModeButton_Click(object sender, RoutedEventArgs e)
    {
        PopMode.IsOpen = true;
    }

    private async void ButtonLike_Click(object sender, RoutedEventArgs e)
    {
        var loadContext = _controller.LastLoadContext;
        if (loadContext is not { PlayItem: { } playItem }) return;

        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var playList = await dbContext.PlayLists.FirstOrDefaultAsync(k => k.IsDefault);
        if (playList == null) return;

        if (loadContext.IsPlayItemFavorite)
        {
            await dbContext.DeletePlayItemsFromPlayListAsync(new[] { playItem }, playList);
            loadContext.IsPlayItemFavorite = false;
        }
        else
        {
            if (await SelectCollectionControl.AddToCollectionAsync(playList, new[] { playItem }))
            {
                loadContext.IsPlayItemFavorite = true;
            }
        }
    }

    private void UserControl_MouseEnter(object sender, MouseEventArgs e)
    {
        OsuBg.Visibility = Visibility.Visible;
    }

    private void UserControl_MouseLeave(object sender, MouseEventArgs e)
    {
        OsuBg.Visibility = Visibility.Hidden;
    }

    private void BgRetc_MouseMove(object sender, MouseEventArgs e)
    {
        //OsuBg.Opacity = 0;
        //RectCover.Opacity = 0.8;
        //BgBorder.Opacity = 1;
        //BlurEffect.Radius = 0;
    }

    private void BgRetc_MouseLeave(object sender, MouseEventArgs e)
    {
        //OsuBg.Opacity = 1;
        //RectCover.Opacity = 1;
        //BgBorder.Opacity = 0;
        //BlurEffect.Radius = 20;
    }
}