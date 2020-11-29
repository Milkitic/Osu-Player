using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Milky.OsuPlayer.UserControls
{
    public class VolumeControlVm : VmBase
    {
        public SharedVm Shared { get; } = SharedVm.Default;
    }

    /// <summary>
    /// VolumeControl.xaml 的交互逻辑
    /// </summary>
    public partial class VolumeControl : UserControl
    {
        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();

        private static readonly SafeDbOperator SafeDbOperator = new SafeDbOperator();

        public VolumeControl()
        {
            InitializeComponent();
        }

        private void VolumeControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_controller != null)
            {
                Offset.Value = _controller.PlayList.CurrentInfo?.BeatmapConfig?.Offset ?? 0;
                _controller.LoadFinished += Controller_LoadFinished;
            }
        }

        private void Controller_LoadFinished(BeatmapContext bc, System.Threading.CancellationToken arg2)
        {
            Offset.Value = bc.BeatmapConfig.Offset;
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

        private void Offset_DragComplete(object sender, DragCompletedEventArgs e)
        {
            if (_controller.PlayList.CurrentInfo == null) return;
            SafeDbOperator.TryUpdateMap(_controller.PlayList.CurrentInfo.Beatmap, _controller.Player.ManualOffset);
        }

        private async void BtnPlayMod_OnClick(object sender, RoutedEventArgs e)
        {
            if (_controller.Player != null)
                await _controller.Player.SetPlayMod((PlayModifier)((Button)sender).Tag);
        }
    }
}
