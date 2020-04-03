using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Shared;
using System;
using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.Shared.Models;

namespace Milky.OsuPlayer.Control
{
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

        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();

        public PlayModeControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _controller.PlayList.PropertyChanged += Player_PropertyChanged;
            SwitchOption(_controller.PlayList.Mode);
        }

        private void Player_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_controller.PlayList.Mode))
            {
                SwitchOption(_controller.PlayList.Mode);
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
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Mode_Changed(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is RadioButton radio)
            {
                _controller.PlayList.Mode = (PlaylistMode)radio.Tag;
                RaiseEvent(new RoutedEventArgs(CloseRequestedEvent, this));
            }
        }
    }
}
