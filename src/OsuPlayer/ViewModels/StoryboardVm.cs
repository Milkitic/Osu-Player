using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Coosu.Database.DataTypes;
using JetBrains.Annotations;

namespace Milki.OsuPlayer.ViewModels;

internal class StoryboardVm : INotifyPropertyChanged
{
    private bool _isScanned;
    private ObservableCollection<Beatmap> _beatmapModels;

    public bool IsScanned
    {
        get => _isScanned;
        set
        {
            if (value == _isScanned) return;
            _isScanned = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Beatmap> BeatmapModels
    {
        get => _beatmapModels;
        set
        {
            if (Equals(value, _beatmapModels)) return;
            _beatmapModels = value;
            OnPropertyChanged();
        }
    }

    public static StoryboardVm Default
    {
        get
        {
            lock (_defaultLock)
            {
                return _default ?? (_default = new StoryboardVm());
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static StoryboardVm _default;
    private static object _defaultLock = new object();
    private StoryboardVm()
    {
    }
}