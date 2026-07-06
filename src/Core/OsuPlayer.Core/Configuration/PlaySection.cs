using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Core.Configuration;

public partial class PlaySection : ObservableObject
{
    public const int OsuFixedOffset = -25;

    public int GeneralOffset { get; set; }

    [JsonIgnore]
    public int GeneralActualOffset => GeneralOffset + OsuFixedOffset;

    public bool ReplacePlayList { get; set; } = true;
    public bool UsePlayerV2 { get; set; } = false;

    [ObservableProperty]
    public partial float PlaybackRate { get; set; } = 1;

    [ObservableProperty]
    public partial bool PlayUseTempo { get; set; }

    public bool AutoPlay { get; set; } = false;
    public bool Memory { get; set; } = true;
    public AudioDeviceDescription DeviceDescription { get; set; }
    public int DesiredLatency { get; set; } = 1;
    public bool IsExclusive { get; set; } = false;
    public PlaylistMode PlayListMode { get; set; } = PlaylistMode.Normal;
}
