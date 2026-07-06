using Avalonia.Controls;
using OsuPlayer.ViewModels.Pages.Settings;

namespace OsuPlayer.Views.Pages.Settings;

public partial class GeneralPage : UserControl
{
    public GeneralPage()
    {
        InitializeComponent();
    }

    public GeneralPage(GeneralPageViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
