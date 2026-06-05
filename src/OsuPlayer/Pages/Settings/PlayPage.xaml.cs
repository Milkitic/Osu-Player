using System.Windows;
using System.Windows.Controls;
using KeyAsio.Core.Audio;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Media.Audio;

namespace OsuPlayer.Pages.Settings;

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
            ApplyFixedAudioDevicePolicy();

            var itemsSource = OsuPlayerAudioDevicePolicy.GetAvailableDevicesAsync(_audioDeviceManager)
                .GetAwaiter()
                .GetResult();
            DeviceInfoCombo.ItemsSource = itemsSource;
            var selectedDevice = OsuPlayerAudioDevicePolicy.SelectOrDefault(
                itemsSource,
                AppSettings.Default.Play.DeviceDescription);
            DeviceInfoCombo.SelectedItem = selectedDevice;
            AppSettings.Default.Play.DeviceDescription = selectedDevice;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void DeviceInfoCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings || e.AddedItems.Count == 0) return;
        var deviceDescription = OsuPlayerAudioDevicePolicy.Normalize((DeviceDescription)e.AddedItems[0]);
        ApplyFixedAudioDevicePolicy();
        AppSettings.Default.Play.DeviceDescription = deviceDescription;
        AppSettings.SaveDefault();

        ApplyDeviceSettingsToEngine(deviceDescription);
    }

    private static void ApplyFixedAudioDevicePolicy()
    {
        AppSettings.Default.Play.DesiredLatency = OsuPlayerAudioDevicePolicy.FixedLatency;
        AppSettings.Default.Play.IsExclusive = OsuPlayerAudioDevicePolicy.UseExclusiveMode;
        AppSettings.Default.Play.DeviceDescription =
            OsuPlayerAudioDevicePolicy.Normalize(AppSettings.Default.Play.DeviceDescription);
    }

    private void ApplyDeviceSettingsToEngine(DeviceDescription deviceDescription)
    {
        if (deviceDescription == null) return;
        try
        {
            OsuPlayerAudioDevicePolicy.StartDevice(_playbackEngine, deviceDescription);
        }
        catch (System.Exception ex)
        {
            Logger.Error(ex, "Error while applying audio device settings.");
        }
    }
}