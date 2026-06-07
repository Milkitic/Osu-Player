//using OsuPlayer.Core.Annotations;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using KeyAsio.Core.Audio;
using OsuPlayer.Presentation.Properties;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Core.Configuration;

public class PlaySection : INotifyPropertyChanged
{
    private bool _playUseTempo;
    private float _playbackRate = 1;

    public int GeneralOffset { get; set; } = -23;

    [JsonIgnore]
    public int GeneralActualOffset => GeneralOffset + 0;

    public bool ReplacePlayList { get; set; } = true;
    public bool UsePlayerV2 { get; set; } = false;

    public float PlaybackRate
    {
        get => _playbackRate;
        set
        {
            if (value.Equals(_playbackRate)) return;
            _playbackRate = value;
            OnPropertyChanged();
        }
    }

    public bool PlayUseTempo
    {
        get => _playUseTempo;
        set
        {
            if (value == _playUseTempo) return;
            _playUseTempo = value;
            OnPropertyChanged();
        }
    }

    public bool AutoPlay { get; set; } = false;
    public bool Memory { get; set; } = true;
    public DeviceDescription DeviceDescription { get; set; } = null;
    public int DesiredLatency { get; set; } = 1;
    public bool IsExclusive { get; set; } = false;
    public PlaylistMode PlayListMode { get; set; } = PlaylistMode.Normal;
    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
