using System.Collections.ObjectModel;
using Coosu.Database.DataTypes;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.ViewModels;

internal class StoryboardVm : SingletonVm<StoryboardVm>
{
    private ObservableCollection<Beatmap> _beatmapModels;
    private bool _isScanned;

    public ObservableCollection<Beatmap> BeatmapModels
    {
        get => _beatmapModels;
        set => this.RaiseAndSetIfChanged(ref _beatmapModels, value);
    }

    public bool IsScanned
    {
        get => _isScanned;
        set => this.RaiseAndSetIfChanged(ref _isScanned, value);
    }
}