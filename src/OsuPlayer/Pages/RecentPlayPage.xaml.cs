using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Pages;

public class RecentPlayPageVm : VmBase
{
    private ObservableCollection<LoosePlayItem> _playItems;

    public ObservableCollection<LoosePlayItem> PlayItems
    {
        get => _playItems;
        set => this.RaiseAndSetIfChanged(ref _playItems, value);
    }

    //public ICommand SearchByConditionCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand<string>(keyword =>
    //        {
    //            WindowEx.GetCurrentFirst<MainWindow>()
    //                .SwitchSearch
    //                .CheckAndAction(page => ((SearchPage)page).Search(keyword));
    //        });
    //    }
    //}

    //public ICommand OpenSourceFolderCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand<OrderedModel<Beatmap>>(orderedModel =>
    //        {
    //            var folder = orderedModel.Model.GetFolder(out _, out _);
    //            if (!Directory.Exists(folder))
    //            {
    //                Notification.Push(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
    //                return;
    //            }

    //            Process.Start(folder);
    //        });
    //    }
    //}

    //public ICommand OpenScorePageCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand<OrderedModel<Beatmap>>(orderedModel =>
    //        {
    //            Process.Start($"https://osu.ppy.sh/s/{orderedModel.Model.BeatmapSetId}");
    //        });
    //    }
    //}

    //public ICommand SaveCollectionCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand<OrderedModel<Beatmap>>(orderedModel =>
    //        {
    //            FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(orderedModel),
    //                DialogOptionFactory.SelectCollectionOptions);
    //        });
    //    }
    //}

    //public ICommand ExportCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand<OrderedModel<Beatmap>>(orderedModel =>
    //        {
    //            if (orderedModel == null) return;
    //            ExportPage.QueueBeatmap(orderedModel);
    //        });
    //    }
    //}

    //public ICommand DirectPlayCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand<OrderedModel<Beatmap>>(async orderedModel =>
    //        {
    //            if (orderedModel == null) return;
    //            await _controller.PlayNewAsync(orderedModel);
    //        });
    //    }
    //}

    //public ICommand PlayCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand<OrderedModel<Beatmap>>(async orderedModel =>
    //        {
    //            if (orderedModel == null) return;
    //            await _controller.PlayNewAsync(orderedModel);
    //        });
    //    }
    //}

    //public ICommand RemoveCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand<OrderedModel<Beatmap>>(async map =>
    //        {
    //            var appDbContext = ServiceProviders.GetApplicationDbContext();
    //            await appDbContext.RemoveBeatmapFromRecent(map);
    //            {
    //                Beatmaps.Remove(map);
    //            }
    //            //await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.Collection, _entries);
    //        });
    //    }
    //}
}

/// <summary>
/// RecentPlayPage.xaml 的交互逻辑
/// </summary>
public partial class RecentPlayPage : Page
{
    private readonly PlayerService _playerService;
    private readonly PlayListService _playListService;
    private readonly RecentPlayPageVm _viewModel;

    public RecentPlayPage()
    {
        InitializeComponent();
        _playerService = App.Current.ServiceProvider.GetService<PlayerService>();
        _playListService = App.Current.ServiceProvider.GetService<PlayListService>();
        DataContext = _viewModel = new RecentPlayPageVm();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await UpdateList();

        var item = _viewModel.PlayItems?.FirstOrDefault(k =>
            k.PlayItem?.StandardizedPath == _playListService.GetCurrentPath());
        if (item != null)
        {
            RecentList.SelectedItem = item;
        }
    }

    private void RecentListItem_MouseDoubleClick(object sender, RoutedEventArgs e)
    {
        PlaySelected();
    }

    private async void BtnDelAll_Click(object sender, RoutedEventArgs e)
    {
        var result = MsgDialog.WarnOkCancel(I18NUtil.GetString("ui-ensureRemoveAll"),
            App.CurrentMainWindow?.Title);
        if (!result) return;

        var appDbContext = ServiceProviders.GetApplicationDbContext();
        await appDbContext.ClearRecentList();
        _viewModel.PlayItems.Clear();
    }

    private async Task UpdateList()
    {
        var appDbContext = ServiceProviders.GetApplicationDbContext();
        var queryResult = await appDbContext.GetRecentListFull();
        // todo: pagination
        _viewModel.PlayItems = new ObservableCollection<LoosePlayItem>(queryResult.Results);
    }

    private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
    {
        var appDbContext = ServiceProviders.GetApplicationDbContext();
        var paginationQueryResult = await appDbContext.GetRecentListFull(0, int.MaxValue);
        if (paginationQueryResult.Results.Count == 0) return;

        await FormUtils.ReplacePlayListAndPlayAll(
            paginationQueryResult.Results.Where(k => !k.IsItemLost).Select(k => k.PlayItem!.StandardizedPath),
            _playListService, _playerService);
    }

    private async void PlaySelected()
    {
        var loosePlayItem = (LoosePlayItem)RecentList.SelectedItem;
        if (loosePlayItem == null) return;
        if (loosePlayItem.IsItemLost) return;

        var standardizedPath = loosePlayItem.PlayItem!.StandardizedPath;
        await _playerService.InitializeNewAsync(standardizedPath, true);
    }
}