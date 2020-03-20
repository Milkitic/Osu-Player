using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Audio.Core;
using Milky.OsuPlayer.ViewModels;
using Milky.WpfApi;
using NAudio.Wave;

namespace Milky.OsuPlayer.Control
{
    public class VolumeControlVm : ViewModelBase
    {
        public SharedVm Shared { get; } = SharedVm.Default;
    }
    /// <summary>
    /// VolumeControl.xaml 的交互逻辑
    /// </summary>
    public partial class VolumeControl : UserControl
    {
        private readonly ObservablePlayController _controller = Services.Get<ObservablePlayController>();

        private readonly AppDbOperator _dbOperator = new AppDbOperator();
        private IWavePlayer _device;

        public VolumeControl()
        {
            InitializeComponent();
        }

        private void VolumeControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            _device = DeviceProvider.GetCurrentDevice();
            if (_device is AsioOut asio)
            {
                BtnAsio.Visibility = Visibility.Visible;
            }
            else
            {
                BtnAsio.Visibility = Visibility.Collapsed;
            }


            Offset.Value = _controller.PlayList.CurrentInfo.BeatmapSettings.Offset;
            _controller.LoadFinished += Controller_LoadFinished;
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
            _controller.Player.HitsoundOffset = (int)Offset.Value;
        }

        private void Offset_DragComplete(object sender, DragCompletedEventArgs e)
        {
            _dbOperator.UpdateMap(_controller.PlayList.CurrentInfo.Beatmap,
                _controller.Player.HitsoundOffset);
        }

        private void BtnAsio_OnClick(object sender, RoutedEventArgs e)
        {
            if (_device is AsioOut asio)
            {
                asio.ShowControlPanel();
            }
        }

        private void BtnPlayMod_OnClick(object sender, RoutedEventArgs e)
        {
            _controller.Player.SetPlayMod((PlayMod)((Button)sender).Tag);
        }
    }
}
