using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.UserControls;

/// <summary>
/// PlayModeControl.xaml 的交互逻辑
/// </summary>
public partial class PlayModeControl : UserControl
{
    public static readonly RoutedEvent CloseRequestedEvent =
        EventManager.RegisterRoutedEvent("CloseRequested",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PlayModeControl));

    public event RoutedEventHandler CloseRequested
    {
        add => AddHandler(CloseRequestedEvent, value);
        remove => RemoveHandler(CloseRequestedEvent, value);
    }

    public PlayModeControl()
    {
        PlayListService = ServiceProviders.Default.GetService<PlayListService>();
        InitializeComponent();
    }

    public PlayListService PlayListService { get; }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        PlayListService.PropertyChanged += Player_PropertyChanged;
        SwitchOption(PlayListService.PlaylistMode);
    }

    private void Player_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayListService.PlaylistMode))
        {
            SwitchOption(PlayListService.PlaylistMode);
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
            default:
                throw new ArgumentOutOfRangeException(nameof(playMode), playMode, null);
        }
    }

    private void Mode_Changed(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is RadioButton radio)
        {
            PlayListService.PlaylistMode = (PlaylistMode)radio.Tag;
            RaiseEvent(new RoutedEventArgs(CloseRequestedEvent, this));
        }
    }
}