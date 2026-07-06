using Avalonia.Controls;
using OsuPlayer.ViewModels.Pages.Settings;

namespace OsuPlayer.Views.Pages.Settings;

public partial class InterfacePage : UserControl
{
    public InterfacePage()
    {
        InitializeComponent();
    }

    public InterfacePage(InterfacePageViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
