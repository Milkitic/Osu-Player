using CommunityToolkit.Mvvm.ComponentModel;

namespace OsuPlayer.Presentation.ObjectModel;

public class NumberableModel : ObservableObject
{
    public int Index { get; internal set; }
}
