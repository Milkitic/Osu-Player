using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    public PlayPageViewModel(
        IAudioDeviceManager audioDeviceManager,
        IPlaybackEngine playbackEngine,
        ObservablePlayController controller)
    {
        _audioDeviceManager = audioDeviceManager;
        _playbackEngine = playbackEngine;
        _controller = controller;
        LoadDevices();
        LoadEffects();
    }

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

    // ===================== Effect section =====================

    public IReadOnlyList<DirectXEffectKind> EffectKinds { get; } =
        Enum.GetValues<DirectXEffectKind>();

    public IReadOnlyList<GargleWaveform> GargleWaveforms { get; } =
        Enum.GetValues<GargleWaveform>();

    private DirectXEffectKind _selectedEffectKind;
    public DirectXEffectKind SelectedEffectKind
    {
        get => _selectedEffectKind;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Effects.Kind == value) return;
            AppSettings.Default.Effects.Kind = value;
            _selectedEffectKind = value;
            OnPropertyChanged();
            RaiseKindDependentProperties();
            AppSettings.SaveDefault();
        }
    }

    private float _effectIntensity;
    public float EffectIntensity
    {
        get => _effectIntensity;
        set
        {
            var clamped = Math.Clamp(value, -1f, 1f);
            if (AppSettings.Default == null || AppSettings.Default.Effects.Intensity.Equals(clamped)) return;
            AppSettings.Default.Effects.Intensity = clamped;
            _effectIntensity = clamped;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    private bool _effectApplyToHitsound = true;
    public bool EffectApplyToHitsound
    {
        get => _effectApplyToHitsound;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Effects.ApplyToHitsound == value) return;
            AppSettings.Default.Effects.ApplyToHitsound = value;
            _effectApplyToHitsound = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    private bool _effectApplyToBackground;
    public bool EffectApplyToBackground
    {
        get => _effectApplyToBackground;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Effects.ApplyToBackground == value) return;
            AppSettings.Default.Effects.ApplyToBackground = value;
            _effectApplyToBackground = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    private bool _effectApplyToMusic;
    public bool EffectApplyToMusic
    {
        get => _effectApplyToMusic;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Effects.ApplyToMusic == value) return;
            AppSettings.Default.Effects.ApplyToMusic = value;
            _effectApplyToMusic = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    // ----- IsSelected helpers (drive per-effect panel visibility) -----
    public bool IsCompressorSelected => _selectedEffectKind == DirectXEffectKind.Compressor;
    public bool IsChorusSelected => _selectedEffectKind == DirectXEffectKind.Chorus;
    public bool IsGargleSelected => _selectedEffectKind == DirectXEffectKind.Gargle;
    public bool IsReverbExSelected => _selectedEffectKind == DirectXEffectKind.ReverbEx;
    public bool IsFlangerSelected => _selectedEffectKind == DirectXEffectKind.Flanger;
    public bool IsDistortionSelected => _selectedEffectKind == DirectXEffectKind.Distortion;

    // ===================== Compressor parameters =====================
    public float CompressorThresholdDb
    {
        get => AppSettings.Default?.Effects.Parameters.Compressor.ThresholdDb ?? 0f;
        set
        {
            if (AppSettings.Default == null) return;
            var p = AppSettings.Default.Effects.Parameters.Compressor;
            if (p.ThresholdDb.Equals(value)) return;
            p.ThresholdDb = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }
    public float CompressorRatio
    {
        get => AppSettings.Default?.Effects.Parameters.Compressor.Ratio ?? 1f;
        set
        {
            if (AppSettings.Default == null) return;
            var p = AppSettings.Default.Effects.Parameters.Compressor;
            if (p.Ratio.Equals(value)) return;
            p.Ratio = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }
    public float CompressorAttackMs
    {
        get => AppSettings.Default?.Effects.Parameters.Compressor.AttackMs ?? 0f;
        set
        {
            if (AppSettings.Default == null) return;
            var p = AppSettings.Default.Effects.Parameters.Compressor;
            if (p.AttackMs.Equals(value)) return;
            p.AttackMs = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }
    public float CompressorReleaseMs
    {
        get => AppSettings.Default?.Effects.Parameters.Compressor.ReleaseMs ?? 0f;
        set
        {
            if (AppSettings.Default == null) return;
            var p = AppSettings.Default.Effects.Parameters.Compressor;
            if (p.ReleaseMs.Equals(value)) return;
            p.ReleaseMs = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }
    public float CompressorMakeupDb
    {
        get => AppSettings.Default?.Effects.Parameters.Compressor.MakeupDb ?? 0f;
        set
        {
            if (AppSettings.Default == null) return;
            var p = AppSettings.Default.Effects.Parameters.Compressor;
            if (p.MakeupDb.Equals(value)) return;
            p.MakeupDb = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    // ===================== Chorus parameters =====================
    public float ChorusVoice1DelayMs
    {
        get => AppSettings.Default?.Effects.Parameters.Chorus.Voice1DelayMs ?? 0f;
        set { UpdateChorus(p => p.Voice1DelayMs = value); }
    }
    public float ChorusVoice2DelayMs
    {
        get => AppSettings.Default?.Effects.Parameters.Chorus.Voice2DelayMs ?? 0f;
        set { UpdateChorus(p => p.Voice2DelayMs = value); }
    }
    public float ChorusVoice3DelayMs
    {
        get => AppSettings.Default?.Effects.Parameters.Chorus.Voice3DelayMs ?? 0f;
        set { UpdateChorus(p => p.Voice3DelayMs = value); }
    }
    public float ChorusDepthMs
    {
        get => AppSettings.Default?.Effects.Parameters.Chorus.DepthMs ?? 0f;
        set { UpdateChorus(p => p.DepthMs = value); }
    }
    public float ChorusRateHz
    {
        get => AppSettings.Default?.Effects.Parameters.Chorus.RateHz ?? 0f;
        set { UpdateChorus(p => p.RateHz = value); }
    }
    public float ChorusWet
    {
        get => AppSettings.Default?.Effects.Parameters.Chorus.Wet ?? 0f;
        set { UpdateChorus(p => p.Wet = value); }
    }

    private void UpdateChorus(Action<Core.Configuration.ChorusParameters> mutate)
    {
        if (AppSettings.Default == null) return;
        var p = AppSettings.Default.Effects.Parameters.Chorus;
        mutate(p);
        AppSettings.SaveDefault();
        OnPropertyChanged(nameof(ChorusVoice1DelayMs));
        OnPropertyChanged(nameof(ChorusVoice2DelayMs));
        OnPropertyChanged(nameof(ChorusVoice3DelayMs));
        OnPropertyChanged(nameof(ChorusDepthMs));
        OnPropertyChanged(nameof(ChorusRateHz));
        OnPropertyChanged(nameof(ChorusWet));
    }

    // ===================== Gargle parameters =====================
    public float GargleRateHz
    {
        get => AppSettings.Default?.Effects.Parameters.Gargle.RateHz ?? 0f;
        set { UpdateGargle(p => p.RateHz = value); }
    }
    public float GargleDepth
    {
        get => AppSettings.Default?.Effects.Parameters.Gargle.Depth ?? 0f;
        set { UpdateGargle(p => p.Depth = value); }
    }
    public GargleWaveform SelectedGargleWaveform
    {
        get => AppSettings.Default?.Effects.Parameters.Gargle.Waveform ?? GargleWaveform.Triangle;
        set
        {
            if (AppSettings.Default == null) return;
            var p = AppSettings.Default.Effects.Parameters.Gargle;
            if (p.Waveform == value) return;
            p.Waveform = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }
    private void UpdateGargle(Action<Core.Configuration.GargleParameters> mutate)
    {
        if (AppSettings.Default == null) return;
        var p = AppSettings.Default.Effects.Parameters.Gargle;
        mutate(p);
        AppSettings.SaveDefault();
        OnPropertyChanged(nameof(GargleRateHz));
        OnPropertyChanged(nameof(GargleDepth));
    }

    // ===================== ReverbEx parameters =====================
    public float ReverbRoomSize
    {
        get => AppSettings.Default?.Effects.Parameters.ReverbEx.RoomSize ?? 0f;
        set { UpdateReverb(p => p.RoomSize = value); }
    }
    public float ReverbDamp
    {
        get => AppSettings.Default?.Effects.Parameters.ReverbEx.Damp ?? 0f;
        set { UpdateReverb(p => p.Damp = value); }
    }
    public float ReverbWet1
    {
        get => AppSettings.Default?.Effects.Parameters.ReverbEx.Wet1 ?? 0f;
        set { UpdateReverb(p => p.Wet1 = value); }
    }
    public float ReverbWet2
    {
        get => AppSettings.Default?.Effects.Parameters.ReverbEx.Wet2 ?? 0f;
        set { UpdateReverb(p => p.Wet2 = value); }
    }
    public float ReverbDry
    {
        get => AppSettings.Default?.Effects.Parameters.ReverbEx.Dry ?? 0f;
        set { UpdateReverb(p => p.Dry = value); }
    }
    public float ReverbWidth
    {
        get => AppSettings.Default?.Effects.Parameters.ReverbEx.Width ?? 0f;
        set { UpdateReverb(p => p.Width = value); }
    }
    private void UpdateReverb(Action<Core.Configuration.ReverbExParameters> mutate)
    {
        if (AppSettings.Default == null) return;
        var p = AppSettings.Default.Effects.Parameters.ReverbEx;
        mutate(p);
        AppSettings.SaveDefault();
        OnPropertyChanged(nameof(ReverbRoomSize));
        OnPropertyChanged(nameof(ReverbDamp));
        OnPropertyChanged(nameof(ReverbWet1));
        OnPropertyChanged(nameof(ReverbWet2));
        OnPropertyChanged(nameof(ReverbDry));
        OnPropertyChanged(nameof(ReverbWidth));
    }

    // ===================== Flanger parameters =====================
    public float FlangerDepthMs
    {
        get => AppSettings.Default?.Effects.Parameters.Flanger.DepthMs ?? 0f;
        set { UpdateFlanger(p => p.DepthMs = value); }
    }
    public float FlangerRateHz
    {
        get => AppSettings.Default?.Effects.Parameters.Flanger.RateHz ?? 0f;
        set { UpdateFlanger(p => p.RateHz = value); }
    }
    public float FlangerFeedback
    {
        get => AppSettings.Default?.Effects.Parameters.Flanger.Feedback ?? 0f;
        set { UpdateFlanger(p => p.Feedback = value); }
    }
    public float FlangerWet
    {
        get => AppSettings.Default?.Effects.Parameters.Flanger.Wet ?? 0f;
        set { UpdateFlanger(p => p.Wet = value); }
    }
    private void UpdateFlanger(Action<Core.Configuration.FlangerParameters> mutate)
    {
        if (AppSettings.Default == null) return;
        var p = AppSettings.Default.Effects.Parameters.Flanger;
        mutate(p);
        AppSettings.SaveDefault();
        OnPropertyChanged(nameof(FlangerDepthMs));
        OnPropertyChanged(nameof(FlangerRateHz));
        OnPropertyChanged(nameof(FlangerFeedback));
        OnPropertyChanged(nameof(FlangerWet));
    }

    // ===================== Distortion parameters =====================
    public float DistortionGainDb
    {
        get => AppSettings.Default?.Effects.Parameters.Distortion.GainDb ?? 0f;
        set { UpdateDistortion(p => p.GainDb = value); }
    }
    public float DistortionCutoffHz
    {
        get => AppSettings.Default?.Effects.Parameters.Distortion.CutoffHz ?? 0f;
        set { UpdateDistortion(p => p.CutoffHz = value); }
    }
    private void UpdateDistortion(Action<Core.Configuration.DistortionParameters> mutate)
    {
        if (AppSettings.Default == null) return;
        var p = AppSettings.Default.Effects.Parameters.Distortion;
        mutate(p);
        AppSettings.SaveDefault();
        OnPropertyChanged(nameof(DistortionGainDb));
        OnPropertyChanged(nameof(DistortionCutoffHz));
    }

    public IRelayCommand ResetEffectCommand => new RelayCommand(ResetCurrentEffect);

    private void ResetCurrentEffect()
    {
        if (AppSettings.Default == null) return;
        EffectParameterSet set;
        switch (_selectedEffectKind)
        {
            case DirectXEffectKind.Compressor:
                AppSettings.Default.Effects.Parameters.Compressor = new CompressorParameters();
                set = AppSettings.Default.Effects.Parameters;
                break;
            case DirectXEffectKind.Chorus:
                AppSettings.Default.Effects.Parameters.Chorus = new ChorusParameters();
                set = AppSettings.Default.Effects.Parameters;
                break;
            case DirectXEffectKind.Gargle:
                AppSettings.Default.Effects.Parameters.Gargle = new GargleParameters();
                set = AppSettings.Default.Effects.Parameters;
                break;
            case DirectXEffectKind.ReverbEx:
                AppSettings.Default.Effects.Parameters.ReverbEx = new ReverbExParameters();
                set = AppSettings.Default.Effects.Parameters;
                break;
            case DirectXEffectKind.Flanger:
                AppSettings.Default.Effects.Parameters.Flanger = new FlangerParameters();
                set = AppSettings.Default.Effects.Parameters;
                break;
            case DirectXEffectKind.Distortion:
                AppSettings.Default.Effects.Parameters.Distortion = new DistortionParameters();
                set = AppSettings.Default.Effects.Parameters;
                break;
            default:
                return;
        }
        AppSettings.SaveDefault();
        RaiseAllParameterProperties();
    }

    private void LoadEffects()
    {
        if (AppSettings.Default == null) return;
        var effects = AppSettings.Default.Effects;
        _selectedEffectKind = effects.Kind;
        _effectIntensity = Math.Clamp(effects.Intensity, -1f, 1f);
        _effectApplyToHitsound = effects.ApplyToHitsound;
        _effectApplyToBackground = effects.ApplyToBackground;
        _effectApplyToMusic = effects.ApplyToMusic;
        OnPropertyChanged(nameof(SelectedEffectKind));
        OnPropertyChanged(nameof(EffectIntensity));
        OnPropertyChanged(nameof(EffectApplyToHitsound));
        OnPropertyChanged(nameof(EffectApplyToBackground));
        OnPropertyChanged(nameof(EffectApplyToMusic));
        RaiseKindDependentProperties();
        RaiseAllParameterProperties();
    }

    private void RaiseKindDependentProperties()
    {
        OnPropertyChanged(nameof(IsCompressorSelected));
        OnPropertyChanged(nameof(IsChorusSelected));
        OnPropertyChanged(nameof(IsGargleSelected));
        OnPropertyChanged(nameof(IsReverbExSelected));
        OnPropertyChanged(nameof(IsFlangerSelected));
        OnPropertyChanged(nameof(IsDistortionSelected));
    }

    private void RaiseAllParameterProperties()
    {
        OnPropertyChanged(nameof(CompressorThresholdDb));
        OnPropertyChanged(nameof(CompressorRatio));
        OnPropertyChanged(nameof(CompressorAttackMs));
        OnPropertyChanged(nameof(CompressorReleaseMs));
        OnPropertyChanged(nameof(CompressorMakeupDb));
        OnPropertyChanged(nameof(ChorusVoice1DelayMs));
        OnPropertyChanged(nameof(ChorusVoice2DelayMs));
        OnPropertyChanged(nameof(ChorusVoice3DelayMs));
        OnPropertyChanged(nameof(ChorusDepthMs));
        OnPropertyChanged(nameof(ChorusRateHz));
        OnPropertyChanged(nameof(ChorusWet));
        OnPropertyChanged(nameof(GargleRateHz));
        OnPropertyChanged(nameof(GargleDepth));
        OnPropertyChanged(nameof(SelectedGargleWaveform));
        OnPropertyChanged(nameof(ReverbRoomSize));
        OnPropertyChanged(nameof(ReverbDamp));
        OnPropertyChanged(nameof(ReverbWet1));
        OnPropertyChanged(nameof(ReverbWet2));
        OnPropertyChanged(nameof(ReverbDry));
        OnPropertyChanged(nameof(ReverbWidth));
        OnPropertyChanged(nameof(FlangerDepthMs));
        OnPropertyChanged(nameof(FlangerRateHz));
        OnPropertyChanged(nameof(FlangerFeedback));
        OnPropertyChanged(nameof(FlangerWet));
        OnPropertyChanged(nameof(DistortionGainDb));
        OnPropertyChanged(nameof(DistortionCutoffHz));
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
