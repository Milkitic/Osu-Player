using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Milky.OsuPlayer.Common;

namespace Milky.OsuPlayer.ViewModels;

internal partial class StoryboardVm : ObservableObject
{
    [ObservableProperty]
    public partial bool IsScanned { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<BeatmapDataModel> BeatmapModels { get; set; }

    public static StoryboardVm Default
    {
        get
        {
            lock (s_defaultLock)
            {
                return _default ??= new StoryboardVm();
            }
        }
    }

    private static StoryboardVm _default;
    private static readonly Lock s_defaultLock = new Lock();

    private StoryboardVm()
    {
    }
}