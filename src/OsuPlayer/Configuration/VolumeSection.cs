using System.Runtime.CompilerServices;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.Configuration;

public class VolumeSection : VmBase
{
    private float _mainVolume = 1.0f;
    private float _musicVolume = 0.5f;
    private float _hitsoundVolume = 0.5f;
    private float _sampleVolume = 0.5f;
    private float _balanceFactor = 0.3f;

    public float MainVolume
    {
        get => _mainVolume;
        set
        {
            var val = _mainVolume;
            SetValue(out _mainVolume, value);
            if (_mainVolume.Equals(val)) return;
            OnPropertyChanged();
        }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            var val = _musicVolume;
            SetValue(out _musicVolume, value);
            if (_musicVolume.Equals(val)) return;
            OnPropertyChanged();
        }
    }
    public float HitsoundVolume
    {
        get => _hitsoundVolume;
        set
        {
            var val = _hitsoundVolume;
            SetValue(out _hitsoundVolume, value);
            if (_hitsoundVolume.Equals(val)) return;
            OnPropertyChanged();
        }
    }

    public float SampleVolume
    {
        get => _sampleVolume;
        set
        {
            var val = _sampleVolume;
            SetValue(out _sampleVolume, value);
            if (_sampleVolume.Equals(val)) return;
            OnPropertyChanged();
        }
    }

    public float BalanceFactor
    {
        get => _balanceFactor * 100;
        set
        {
            var val = _balanceFactor;
            SetValue(out _balanceFactor, value / 100f);
            if (_balanceFactor.Equals(val)) return;
            OnPropertyChanged();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetValue(out float source, float value) =>
        source = value switch { < 0 => 0, > 1 => 1, _ => value };
}