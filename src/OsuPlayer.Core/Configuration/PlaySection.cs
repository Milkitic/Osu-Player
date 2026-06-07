using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyAsio.Core.Audio;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Core.Configuration;

public partial class PlaySection : ObservableObject
{
    public int GeneralOffset { get; set; } = -23;

    [JsonIgnore]
    public int GeneralActualOffset => GeneralOffset + 0;

    public bool ReplacePlayList { get; set; } = true;
    public bool UsePlayerV2 { get; set; } = false;

    [ObservableProperty]
    public partial float PlaybackRate { get; set; } = 1;

    [ObservableProperty]
    public partial bool PlayUseTempo { get; set; }

    public bool AutoPlay { get; set; } = false;
    public bool Memory { get; set; } = true;
    public DeviceDescription DeviceDescription { get; set; } = null;
    public int DesiredLatency { get; set; } = 1;
    public bool IsExclusive { get; set; } = false;
    public PlaylistMode PlayListMode { get; set; } = PlaylistMode.Normal;
}
