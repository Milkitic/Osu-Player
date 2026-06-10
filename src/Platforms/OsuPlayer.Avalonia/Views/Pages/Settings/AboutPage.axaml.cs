using Avalonia.Controls;
using OsuPlayer.ViewModels.Pages.Settings;

namespace OsuPlayer.Views.Pages.Settings;

public partial class AboutPage : UserControl
{
    public AboutPage()
    {
        InitializeComponent();
    }

    public AboutPage(AboutPageViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
