#nullable enable

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.ViewModels;

namespace Milki.OsuPlayer.UserControls;

/// <summary>
/// SelectCollectionControl.xaml 的交互逻辑
/// </summary>
public partial class SelectCollectionControl : UserControl
{
    private readonly SelectCollectionPageViewModel _viewModel;
    private readonly ContentDialog _overlay;

    public SelectCollectionControl(PlayItem playItem) : this(new[] { playItem })
    {
    }

    public SelectCollectionControl(IList<PlayItem> playItems)
    {
        DataContext = _viewModel = new SelectCollectionPageViewModel();
        _viewModel.PlayItems = playItems;
        InitializeComponent();
        _overlay = App.CurrentMainContentDialog.GetOrCreateSubOverlay();
        _overlay.DialogPadding = new Thickness();
    }

    private async void SelectCollectionControl_OnInitialized(object? sender, EventArgs e)
    {
        await RefreshList();
    }

    private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
    {
        var addCollectionControl = new AddCollectionControl();
        _overlay.ShowContent(addCollectionControl, DialogOptionFactory.AddCollectionOptions, async (obj, args) =>
        {
            await using var dbContext = ServiceProviders.GetApplicationDbContext();
            await dbContext.AddPlayListAsync(addCollectionControl.CollectionName.Text);
            await SharedVm.Default.UpdatePlayListsAsync();

            await RefreshList();
        });
    }

    private async void BtnSelect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: PlayList playList }) return;
        await CommonUtils.AddToCollectionAsync(playList, _viewModel.PlayItems);
        App.CurrentMainContentDialog.RaiseOk();
    }

    private async Task RefreshList()
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        _viewModel.PlayLists = new ObservableCollection<PlayList>(await dbContext.GetPlayListsAsync());
    }
}