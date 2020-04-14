using Milky.OsuPlayer.Presentation.Interaction;

namespace Milky.OsuPlayer.ViewModels
{
    public class ListPageViewModel : VmBase
    {
        public ListPageViewModel(int index)
        {
            Index = index;
        }

        public int Index { get; set; }
        public bool IsActivated { get; set; }
    }
}