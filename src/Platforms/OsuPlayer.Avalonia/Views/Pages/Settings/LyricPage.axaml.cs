using Avalonia.Controls;
using OsuPlayer.ViewModels.Pages.Settings;

namespace OsuPlayer.Views.Pages.Settings;

public partial class LyricPage : UserControl
{
    public LyricPage()
    {
        InitializeComponent();
    }

    public LyricPage(LyricPageViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
