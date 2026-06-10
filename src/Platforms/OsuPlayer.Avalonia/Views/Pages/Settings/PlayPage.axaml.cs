using Avalonia.Controls;
using OsuPlayer.ViewModels.Pages.Settings;

namespace OsuPlayer.Views.Pages.Settings;

public partial class PlayPage : UserControl
{
    public PlayPage()
    {
        InitializeComponent();
    }

    public PlayPage(PlayPageViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
