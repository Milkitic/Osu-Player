using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Services;
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

    private readonly PlayListService _playListService;

    public PlayModeControl()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            _playListService = ServiceProviders.Default.GetService<PlayListService>();
        }

        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            _playListService.PropertyChanged += Player_PropertyChanged;
            SwitchOption(_playListService.PlayListMode);
        }
    }

    private void Player_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_playListService.PlayListMode))
        {
            SwitchOption(_playListService.PlayListMode);
        }
    }

    private void SwitchOption(PlayListMode playMode)
    {
        switch (playMode)
        {
            case PlayListMode.Normal:
                ModeNormal.IsChecked = true;
                break;
            case PlayListMode.Random:
                ModeRandom.IsChecked = true;
                break;
            case PlayListMode.Loop:
                ModeLoop.IsChecked = true;
                break;
            case PlayListMode.LoopRandom:
                ModeLoopRandom.IsChecked = true;
                break;
            case PlayListMode.Single:
                ModeSingle.IsChecked = true;
                break;
            case PlayListMode.SingleLoop:
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
            _playListService.PlayListMode = (PlayListMode)radio.Tag;
            RaiseEvent(new RoutedEventArgs(CloseRequestedEvent, this));
        }
    }
}