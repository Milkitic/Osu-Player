using Avalonia.Controls;
using OsuPlayer.ViewModels.Pages.Settings;

namespace OsuPlayer.Views.Pages.Settings;

public partial class ExportPage : UserControl
{
    public ExportPage()
    {
        InitializeComponent();
    }

    public ExportPage(ExportPageViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
