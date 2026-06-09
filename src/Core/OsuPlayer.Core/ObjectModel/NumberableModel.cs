using CommunityToolkit.Mvvm.ComponentModel;

namespace OsuPlayer.Core.ObjectModel;

public class NumberableModel : ObservableObject
{
    public int Index { get; internal set; }
}
