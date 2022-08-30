using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.UiComponents.ContentDialogComponent;

namespace Milki.OsuPlayer.UserControls;

public class SelectPlayListControlVm : VmBase
{
    private IList<PlayItem> _playItems;
    private ObservableCollection<PlayList> _playLists;

    public IList<PlayItem> PlayItems
    {
        get => _playItems;
        set => this.RaiseAndSetIfChanged(ref _playItems, value);
    }

    public ObservableCollection<PlayList> PlayLists
    {
        get => _playLists;
        set => this.RaiseAndSetIfChanged(ref _playLists, value);
    }
}

/// <summary>
/// SelectCollectionControl.xaml 的交互逻辑
/// </summary>
public partial class SelectPlayListControl : UserControl
{
    private readonly SelectPlayListControlVm _viewModel;
    private readonly ContentDialog _overlay;

    public SelectPlayListControl(PlayItem playItem) : this(new[] { playItem })
    {
    }

    public SelectPlayListControl(IList<PlayItem> playItems)
    {
        DataContext = _viewModel = new SelectPlayListControlVm();
        _viewModel.PlayItems = playItems;
        InitializeComponent();
        _overlay = App.CurrentMainContentDialog.GetOrCreateSubOverlay();
        _overlay.DialogPadding = new Thickness();
    }

    private async void SelectCollectionControl_OnInitialized(object sender, EventArgs e)
    {
        await RefreshList();
    }

    private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
    {
        var addCollectionControl = new AddPlayListControl();
        _overlay.ShowContent(addCollectionControl, DialogOptionFactory.AddCollectionOptions, async (obj, args) =>
        {
            await using var dbContext = ServiceProviders.GetApplicationDbContext();
            await dbContext.AddPlayListAsync(addCollectionControl.TbPlayListName.Text);
            await SharedVm.Default.UpdatePlayListsAsync();

            await RefreshList();
        });
    }

    private async void BtnSelect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: PlayList playList }) return;
        await CommonUtils.AddToPlayListAsync(playList, _viewModel.PlayItems);
        App.CurrentMainContentDialog.RaiseOk();
    }

    private async ValueTask RefreshList()
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        _viewModel.PlayLists = new ObservableCollection<PlayList>(await dbContext.GetPlayListsAsync());
    }
}