using System.Linq;
using System.Windows;
using System.Windows.Controls;
using KeyAsio.Core.Audio;
using Milky.OsuPlayer.Core.Configuration;
using NAudio.Wave;

namespace Milky.OsuPlayer.Pages.Settings
{
    /// <summary>
    /// PlayPage.xaml 的交互逻辑
    /// </summary>
    public partial class PlayPage : Page
    {
        private readonly IAudioDeviceManager _audioDeviceManager;
        private readonly IPlaybackEngine _playbackEngine;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _isLoadingSettings;

        public PlayPage(IAudioDeviceManager audioDeviceManager, IPlaybackEngine playbackEngine)
        {
            _audioDeviceManager = audioDeviceManager;
            _playbackEngine = playbackEngine;
            InitializeComponent();
        }

        private void SliderOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AppSettings.Default.Play.GeneralOffset = (int)SliderOffset.Value;
            BoxOffset.Text = AppSettings.Default.Play.GeneralOffset.ToString();
            AppSettings.SaveDefault();
        }

        private void BoxOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(BoxOffset.Text, out var num))
                return;
            if (num > SliderOffset.Maximum)
            {
                num = (int)SliderOffset.Maximum;
                AppSettings.Default.Play.GeneralOffset = num;
                BoxOffset.Text = AppSettings.Default.Play.GeneralOffset.ToString();
            }
            else if (num < SliderOffset.Minimum)
            {
                num = (int)SliderOffset.Minimum;
                AppSettings.Default.Play.GeneralOffset = num;
                BoxOffset.Text = AppSettings.Default.Play.GeneralOffset.ToString();
            }

            AppSettings.Default.Play.GeneralOffset = num;
            SliderOffset.Value = AppSettings.Default.Play.GeneralOffset;
            AppSettings.SaveDefault();
        }

        private void RadioReplace_Checked(object sender, RoutedEventArgs e)
        {
            AppSettings.Default.Play.ReplacePlayList = true;
            AppSettings.SaveDefault();
        }

        private void RadioInsert_Checked(object sender, RoutedEventArgs e)
        {
            AppSettings.Default.Play.ReplacePlayList = false;
            AppSettings.SaveDefault();
        }

        private void ChkAutoPlay_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!ChkAutoPlay.IsChecked.HasValue)
                return;
            AppSettings.Default.Play.AutoPlay = ChkAutoPlay.IsChecked.Value;
            AppSettings.SaveDefault();
        }

        private void ChkMemory_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!ChkMemory.IsChecked.HasValue)
                return;
            AppSettings.Default.Play.Memory = ChkMemory.IsChecked.Value;
            AppSettings.SaveDefault();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoadingSettings = true;
            try
            {
                SliderOffset.Value = AppSettings.Default.Play.GeneralOffset;
                BoxOffset.Text = AppSettings.Default.Play.GeneralOffset.ToString();
                if (AppSettings.Default.Play.ReplacePlayList)
                    RadioReplace.IsChecked = true;
                else
                    RadioInsert.IsChecked = true;
                ChkAutoPlay.IsChecked = AppSettings.Default.Play.AutoPlay;
                ChkMemory.IsChecked = AppSettings.Default.Play.Memory;
                SliderLatency.Value = AppSettings.Default.Play.DesiredLatency;
                BoxLatency.Text = AppSettings.Default.Play.DesiredLatency.ToString();
                var itemsSource = _audioDeviceManager.GetCachedAvailableDevicesAsync().GetAwaiter().GetResult();
                DeviceInfoCombo.ItemsSource = itemsSource;
                if (itemsSource.Contains(AppSettings.Default.Play.DeviceDescription, DeviceComparer.Instance))
                {
                    DeviceInfoCombo.SelectedItem = itemsSource.First(k =>
                        DeviceComparer.Instance.Equals(k, AppSettings.Default.Play.DeviceDescription));
                }
                else
                {
                    DeviceInfoCombo.SelectedIndex = 0;
                }

                var selectedItem = (DeviceDescription)DeviceInfoCombo.SelectedItem;
                SliderLatency.IsEnabled = selectedItem.WavePlayerType != WavePlayerType.ASIO;
            }
            finally
            {
                _isLoadingSettings = false;
            }
        }

        private void BoxLatency_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingSettings) return;
            if (!int.TryParse(BoxLatency.Text, out var num))
                return;
            if (num > SliderLatency.Maximum)
            {
                num = (int)SliderLatency.Maximum;
                AppSettings.Default.Play.DesiredLatency = num;
                BoxLatency.Text = AppSettings.Default.Play.DesiredLatency.ToString();
            }
            else if (num < SliderLatency.Minimum)
            {
                num = (int)SliderLatency.Minimum;
                AppSettings.Default.Play.DesiredLatency = num;
                BoxLatency.Text = AppSettings.Default.Play.DesiredLatency.ToString();
            }

            AppSettings.Default.Play.DesiredLatency = num;
            UpdateSelectedDeviceLatency();
            SliderLatency.Value = AppSettings.Default.Play.DesiredLatency;
            AppSettings.SaveDefault();

            ApplyDeviceSettingsToEngine(AppSettings.Default.Play.DeviceDescription);
        }

        private void SliderLatency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isLoadingSettings) return;
            AppSettings.Default.Play.DesiredLatency = (int)SliderLatency.Value;
            UpdateSelectedDeviceLatency();
            BoxLatency.Text = AppSettings.Default.Play.DesiredLatency.ToString();
            AppSettings.SaveDefault();

            ApplyDeviceSettingsToEngine(AppSettings.Default.Play.DeviceDescription);
        }

        private void DeviceInfoCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingSettings || e.AddedItems.Count == 0) return;
            var newVal = (DeviceDescription)e.AddedItems[0];
            SliderLatency.IsEnabled = newVal.WavePlayerType != WavePlayerType.ASIO;
            var deviceDescription = newVal with
            {
                Latency = AppSettings.Default.Play.DesiredLatency,
                IsExclusive = AppSettings.Default.Play.IsExclusive
            };
            AppSettings.Default.Play.DeviceDescription = deviceDescription;
            AppSettings.SaveDefault();

            ApplyDeviceSettingsToEngine(deviceDescription);
        }

        private static void UpdateSelectedDeviceLatency()
        {
            if (AppSettings.Default.Play.DeviceDescription == null) return;
            AppSettings.Default.Play.DeviceDescription = AppSettings.Default.Play.DeviceDescription with
            {
                Latency = AppSettings.Default.Play.DesiredLatency,
                IsExclusive = AppSettings.Default.Play.IsExclusive
            };
        }

        private void ApplyDeviceSettingsToEngine(DeviceDescription deviceDescription)
        {
            if (deviceDescription == null) return;
            try
            {
                _playbackEngine.StartDevice(deviceDescription, new WaveFormat(44100, 2));
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex, "Error while applying audio device settings.");
            }
        }
    }
}
