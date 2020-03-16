using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.ViewModels;
using Milky.WpfApi;

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

        private readonly ObservablePlayController _controller = Services.Get<ObservablePlayController>();

        public PlayModeControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _controller.PlayList.PropertyChanged += Player_PropertyChanged;
            SwitchOption(_controller.PlayList.PlayMode);
        }

        private void Player_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_controller.PlayList.PlayMode))
            {
                SwitchOption(_controller.PlayList.PlayMode);
            }
        }

        private void SwitchOption(PlayMode playMode)
        {
            switch (playMode)
            {
                case PlayMode.Normal:
                    ModeNormal.IsChecked = true;
                    break;
                case PlayMode.Random:
                    ModeRandom.IsChecked = true;
                    break;
                case PlayMode.Loop:
                    ModeLoop.IsChecked = true;
                    break;
                case PlayMode.LoopRandom:
                    ModeLoopRandom.IsChecked = true;
                    break;
                case PlayMode.Single:
                    ModeSingle.IsChecked = true;
                    break;
                case PlayMode.SingleLoop:
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
                _controller.PlayList.PlayMode = (PlayMode)radio.Tag;
                RaiseEvent(new RoutedEventArgs(CloseRequestedEvent, this));
            }
        }
    }
}
