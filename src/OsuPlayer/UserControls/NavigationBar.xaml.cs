using System.Windows;
using System.Windows.Controls;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.UserControls;

/// <summary>
/// NavigationBar.xaml 的交互逻辑
/// </summary>
public partial class NavigationBar : UserControl
{
    private readonly SharedVm _viewModel;
    private MainWindow _mainWindow;

    public NavigationBar()
    {
        DataContext = _viewModel = SharedVm.Default;
        InitializeComponent();
    }

    private void NavigationBar_OnLoaded(object sender, RoutedEventArgs e)
    {
        SwitchSearch.IsChecked = true;
        _mainWindow = App.Current?.MainWindow as MainWindow;
    }

    private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
    {
        var addCollectionControl = new AddCollectionControl();
        _mainWindow.FrontDialogOverlay.ShowContent(addCollectionControl, DialogOptionFactory.AddCollectionOptions, async (obj, args) =>
        {
            await using var applicationDbContext = ServiceProviders.GetApplicationDbContext();
            await applicationDbContext.AddPlayListAsync(addCollectionControl.CollectionName.Text); //todo: exists
            await SharedVm.Default.UpdatePlayListsAsync();
        });
    }

    private async void BtnNavigationTrigger_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsNavigationCollapsed = !_viewModel.IsNavigationCollapsed;
        await using var applicationDbContext = ServiceProviders.GetApplicationDbContext();
        var softwareState = await applicationDbContext.GetSoftwareState();
        softwareState.ShowFullNavigation = !_viewModel.IsNavigationCollapsed;
        await applicationDbContext.UpdateAndSaveChangesAsync(softwareState, k => k.ShowFullNavigation);
    }
}