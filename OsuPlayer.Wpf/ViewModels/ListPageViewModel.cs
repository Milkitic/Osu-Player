using Milky.WpfApi;

namespace Milky.OsuPlayer.ViewModels {
    public class ListPageViewModel : ViewModelBase
    {
        public ListPageViewModel(int index)
        {
            Index = index;
        }

        public int Index { get; set; }
        public bool IsActivated { get; set; }
    }
}