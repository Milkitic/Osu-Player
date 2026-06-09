using System.Collections.Generic;
using System.ComponentModel;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Shared;

public interface IUserPreferences : INotifyPropertyChanged
{
    float VolumeMain { get; set; }
    float VolumeMusic { get; set; }
    float VolumeHitsound { get; set; }
    float VolumeSample { get; set; }
    float VolumeBalanceFactor { get; set; }
    float PlaybackRate { get; set; }
    bool PlayUseTempo { get; set; }
    AudioDeviceDescription PlayDeviceDescription { get; set; }
    int PlayGeneralActualOffset { get; }
    PlaylistMode PlayListMode { get; set; }

    HashSet<MapIdentity> CurrentList { get; set; }
    MapIdentity? CurrentMap { get; set; }

    void Save();
}
