using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xaml;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.ViewModels;

public class SearchPageViewModel : VmBase
{
    private ObservableCollection<PlayGroupQuery> _playItems;
    private List<ListPageViewModel> _pages;
    private ListPageViewModel _lastPage;
    private ListPageViewModel _firstPage;
    private ListPageViewModel _currentPage;
    private string _searchText;

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public ObservableCollection<PlayGroupQuery> PlayItems
    {
        get => _playItems;
        set => this.RaiseAndSetIfChanged(ref _playItems, value);
    }

    public List<ListPageViewModel> Pages
    {
        get => _pages;
        set => this.RaiseAndSetIfChanged(ref _pages, value);
    }

    public ListPageViewModel LastPage
    {
        get => _lastPage;
        set => this.RaiseAndSetIfChanged(ref _lastPage, value);
    }

    public ListPageViewModel FirstPage
    {
        get => _firstPage;
        set => this.RaiseAndSetIfChanged(ref _firstPage, value);
    }

    public ListPageViewModel CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    //public VirtualizingGalleryWrapPanel GalleryWrapPanel { get; set; }

    //public ICommand SwitchCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand(obj =>
    //        {
    //            if (obj is bool b)
    //            {
    //                if (CurrentPage == null) return;
    //                var page = b ? GetPage(CurrentPage.Index + 1) : GetPage(CurrentPage.Index - 1);
    //                if (page == null) return;
    //                if (page.IsActivated)
    //                {
    //                    return;
    //                }

    //                SetPage(PlayItems.Count, page.Index - 1);
    //            }
    //            else
    //            {
    //                var reqPage = (int)obj;
    //                var page = GetPage(reqPage);
    //                if (page.IsActivated)
    //                {
    //                    return;
    //                }

    //                SetPage(PlayItems.Count, reqPage - 1);
    //            }
    //        });
    //    }
    //}

    //public ICommand SearchByConditionCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand(param =>
    //        {
    //            WindowEx.GetCurrentFirst<MainWindow>()
    //                .SwitchSearch
    //                .CheckAndAction(page => ((SearchPage)page).Search((string)param));
    //        });
    //    }
    //}

    //public ICommand OpenSourceFolderCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand(async param =>
    //        {
    //            var beatmap = (Beatmap)param;
    //            var map = await GetHighestSrBeatmap(beatmap);
    //            if (map == null) return;
    //            var folderName = beatmap.GetFolder(out _, out _);
    //            if (!Directory.Exists(folderName))
    //            {
    //                Notification.Push(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
    //                return;
    //            }

    //            Process.Start(folderName);
    //        });
    //    }
    //}

    //public ICommand OpenScorePageCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand(async param =>
    //        {
    //            var beatmap = (Beatmap)param;
    //            var map = await GetHighestSrBeatmap(beatmap);
    //            if (map == null) return;
    //            Process.Start($"https://osu.ppy.sh/s/{map.BeatmapSetId}");
    //        });
    //    }
    //}

    //public ICommand SaveCollectionCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand(async param =>
    //        {
    //            var beatmap = (Beatmap)param;

    //            await using var dbContext = ServiceProviders.GetApplicationDbContext();
    //            var beatmaps = await dbContext.GetBeatmapsFromFolder(beatmap.FolderNameOrPath, beatmap.InOwnDb);

    //            var control = new DiffSelectControl(
    //                beatmaps, (selected, arg) =>
    //                {
    //                    arg.Handled = true;
    //                    FrontDialogOverlay.Default.ShowContent(
    //                        new SelectCollectionControl(selected),
    //                        DialogOptionFactory.SelectCollectionOptions
    //                    );
    //                });
    //            FrontDialogOverlay.Default.ShowContent(control, DialogOptionFactory.DiffSelectOptions);
    //        });
    //    }
    //}

    //public ICommand ExportCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand(async param =>
    //        {
    //            var beatmap = (Beatmap)param;
    //            var map = await GetHighestSrBeatmap(beatmap);
    //            if (map == null) return;
    //            ExportPage.QueueBeatmap(map);
    //        });
    //    }
    //}

    //public ICommand DirectPlayCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand(async param =>
    //        {
    //            var beatmap = (Beatmap)param;
    //            var map = await GetHighestSrBeatmap(beatmap);
    //            if (map == null) return;
    //            var controller = Service.Get<ObservablePlayController>();
    //            await controller.PlayNewAsync(map);
    //        });
    //    }
    //}

    //public ICommand PlayCommand
    //{
    //    get
    //    {
    //        return new DelegateCommand(async param =>
    //        {
    //            var beatmap = (Beatmap)param;
    //            await using var dbContext = ServiceProviders.GetApplicationDbContext();
    //            var beatmaps = await dbContext.GetBeatmapsFromFolder(beatmap.FolderNameOrPath, beatmap.InOwnDb);

    //            var control = new DiffSelectControl(
    //                beatmaps, async (selected, arg) =>
    //                {
    //                    var controller = Service.Get<ObservablePlayController>();
    //                    await controller.PlayNewAsync(selected, true);
    //                });
    //            FrontDialogOverlay.Default.ShowContent(control, DialogOptionFactory.DiffSelectOptions);
    //        });
    //    }
    //}

    //private async Task<Beatmap> GetHighestSrBeatmap(Beatmap beatmap)
    //{
    //    if (beatmap == null) return null;

    //    await using var dbContext = ServiceProviders.GetApplicationDbContext();
    //    var map = (await dbContext.GetBeatmapsFromFolder(beatmap.FolderNameOrPath, beatmap.InOwnDb))
    //        .GetHighestDiff();
    //    return map;
    //}
}

[MarkupExtensionReturnType(typeof(ContentControl))]
public class RootObject : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var rootObjectProvider = (IRootObjectProvider)serviceProvider.GetService(typeof(IRootObjectProvider));
        return rootObjectProvider?.RootObject;
    }
}