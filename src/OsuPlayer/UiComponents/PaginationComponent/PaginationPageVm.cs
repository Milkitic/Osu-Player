using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.UiComponents.PaginationComponent;

public sealed class PaginationPageVm : VmBase
{
    public PaginationPageVm(int index)
    {
        Index = index;
    }

    public int Index { get; set; }
    public bool IsActivated { get; set; }
}