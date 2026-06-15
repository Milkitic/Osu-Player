using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyAsio.Core.Audio;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Media.Audio;
using OsuPlayer.Playback;

namespace OsuPlayer.ViewModels.Pages.Settings;

public partial class PlayPageViewModel : ObservableObject
{
    private readonly IAudioDeviceManager _audioDeviceManager;
    private readonly IPlaybackEngine _playbackEngine;
    private readonly ObservablePlayController _controller;

    private bool _isInitializing;

    public int GeneralOffset
    {
        get => AppSettings.Default?.Play.GeneralOffset ?? 0;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Play.GeneralOffset == value) return;
            AppSettings.Default.Play.GeneralOffset = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
            if (_controller.Player != null)
            {
                _controller.Player.GeneralOffset = AppSettings.Default.Play.GeneralActualOffset;
            }
        }
    }

    public bool ReplacePlayList
    {
        get => AppSettings.Default?.Play.ReplacePlayList == true;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Play.ReplacePlayList == value) return;
            AppSettings.Default.Play.ReplacePlayList = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(InsertPlayList));
            AppSettings.SaveDefault();
        }
    }

    public bool InsertPlayList
    {
        get => !(AppSettings.Default?.Play.ReplacePlayList ?? true);
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Play.ReplacePlayList == !value) return;
            AppSettings.Default.Play.ReplacePlayList = !value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ReplacePlayList));
            AppSettings.SaveDefault();
        }
    }

    public bool AutoPlay
    {
        get => AppSettings.Default?.Play.AutoPlay == true;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Play.AutoPlay == value) return;
            AppSettings.Default.Play.AutoPlay = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    public bool Memory
    {
        get => AppSettings.Default?.Play.Memory != false;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Play.Memory == value) return;
            AppSettings.Default.Play.Memory = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    [ObservableProperty]
    public partial IReadOnlyList<DeviceDescription> AvailableDevices { get; set; } = [];

    private DeviceDescription? _selectedDevice;
    public DeviceDescription? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (!SetProperty(ref _selectedDevice, value) || _isInitializing || value == null || AppSettings.Default == null)
            {
                return;
            }

            var normalized = OsuPlayerAudioDevicePolicy.Normalize(value);
            ApplyFixedAudioDevicePolicy();
            AppSettings.Default.Play.DeviceDescription = OsuPlayerAudioDevicePolicy.ToConfiguration(normalized);
            AppSettings.SaveDefault();
            ApplyDeviceSettingsToEngine(normalized);
        }
    }

    public PlayPageViewModel(
        IAudioDeviceManager audioDeviceManager,
        IPlaybackEngine playbackEngine,
        ObservablePlayController controller)
    {
        _audioDeviceManager = audioDeviceManager;
        _playbackEngine = playbackEngine;
        _controller = controller;
        LoadDevices();
    }

    private void LoadDevices()
    {
        _isInitializing = true;
        try
        {
            ApplyFixedAudioDevicePolicy();
            var itemsSource = OsuPlayerAudioDevicePolicy.GetAvailableDevicesAsync(_audioDeviceManager)
                .GetAwaiter()
                .GetResult();
            AvailableDevices = itemsSource;
            var initial = OsuPlayerAudioDevicePolicy.SelectOrDefault(
                itemsSource,
                OsuPlayerAudioDevicePolicy.FromConfiguration(AppSettings.Default?.Play.DeviceDescription));
            SelectedDevice = initial;
            if (AppSettings.Default != null)
            {
                AppSettings.Default.Play.DeviceDescription = OsuPlayerAudioDevicePolicy.ToConfiguration(initial);
            }
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private static void ApplyFixedAudioDevicePolicy()
    {
        if (AppSettings.Default == null) return;
        AppSettings.Default.Play.DesiredLatency = OsuPlayerAudioDevicePolicy.RecommendedLatency;
        AppSettings.Default.Play.IsExclusive = OsuPlayerAudioDevicePolicy.UseExclusiveMode;
        AppSettings.Default.Play.DeviceDescription =
            OsuPlayerAudioDevicePolicy.ToConfiguration(
                OsuPlayerAudioDevicePolicy.FromConfiguration(AppSettings.Default.Play.DeviceDescription));
    }

    private void ApplyDeviceSettingsToEngine(DeviceDescription deviceDescription)
    {
        try
        {
            OsuPlayerAudioDevicePolicy.StartDevice(_playbackEngine, deviceDescription);
        }
        catch (Exception)
        {
        }
    }
}
