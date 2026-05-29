using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xaml;
using CommunityToolkit.Mvvm.Input;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.Shared.Models;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Milky.OsuPlayer.UiComponents.PanelComponent;
using Milky.OsuPlayer.UserControls;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.ViewModels
{
    public class SearchPageViewModel : VmBase
    {
        private readonly IPlayerDataService _playerData;

        private const int MaxListCount = 250;
        private const int QueryDelayMs = 167;

        private List<BeatmapDataModel> _displayedMaps = [];
        private List<ListPageViewModel> _pages = [];
        private ListPageViewModel _currentPage;
        private List<Beatmap> _searchedDbMaps = [];
        private string _searchText;
        private CancellationTokenSource _queryCancellation;
        private int _queryVersion;

        public List<BeatmapDataModel> DisplayedMaps
        {
            get => _displayedMaps;
            private set
            {
                _displayedMaps = value;
                OnPropertyChanged();
            }
        }

        public List<ListPageViewModel> Pages
        {
            get => _pages;
            private set
            {
                _pages = value;
                OnPropertyChanged();
            }
        }

        public ListPageViewModel CurrentPage
        {
            get => _currentPage;
            private set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        public VirtualizingGalleryWrapPanel GalleryWrapPanel { get; set; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        // Stores the currently displayed page results so existing page actions can reuse them.
        public List<Beatmap> SearchedDbMaps
        {
            get => _searchedDbMaps;
            private set
            {
                _searchedDbMaps = value;
                OnPropertyChanged();
            }
        }

        public SearchPageViewModel()
            : this(AppServices.PlayerData)
        {
        }

        public SearchPageViewModel(IPlayerDataService playerData)
        {
            _playerData = playerData;
        }

        public async Task PlayListQueryAsync(int pageIndex = 0, bool debounce = true)
        {
            var normalizedPageIndex = Math.Max(0, pageIndex);
            var requestVersion = Interlocked.Increment(ref _queryVersion);
            var cancellation = BeginQuery();

            try
            {
                if (debounce)
                {
                    await Task.Delay(QueryDelayMs, cancellation.Token);
                }

                var result = await _playerData.SearchBeatmapPageAsync(SearchText, BeatmapSortMode.Artist,
                    normalizedPageIndex * MaxListCount, MaxListCount);
                if (cancellation.IsCancellationRequested || requestVersion != _queryVersion)
                {
                    return;
                }

                SearchedDbMaps = result.Results.ToList();
                GalleryWrapPanel?.ClearNotificationCount();
                DisplayedMaps = SearchedDbMaps.ToDataModelList(true);
                SetPage(result.TotalCount, normalizedPageIndex);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                EndQuery(cancellation);
            }
        }

        public Task<List<Beatmap>> GetAllMatchedBeatmapsAsync()
        {
            return _playerData.SearchBeatmapByOptionsAsync(SearchText, BeatmapSortMode.Artist, 0, int.MaxValue);
        }

        private void SetPage(int totalCount, int nowIndex)
        {
            totalCount = (int)Math.Ceiling(totalCount / (float)MaxListCount);
            if (totalCount <= 0)
            {
                Pages = [];
                CurrentPage = null;
                return;
            }

            int count, startIndex;
            if (totalCount > 10)
            {
                if (nowIndex > 5)
                {
                    if (nowIndex >= totalCount - 5)
                    {
                        startIndex = totalCount - 10;
                    }
                    else
                    {
                        startIndex = nowIndex - 5;
                    }
                }
                else
                {
                    startIndex = 0;
                }

                count = 10;
            }
            else
            {
                count = totalCount;
                startIndex = 0;
            }

            var pages = new List<ListPageViewModel>(totalCount);
            for (int i = startIndex; i < startIndex + count; i++)
            {
                pages.Add(new ListPageViewModel(i + 1));
            }

            Pages = pages;
            ListPageViewModel page = GetPage(nowIndex + 1);

            if (page != null)
                page.IsActivated = true;

            CurrentPage = page;
        }

        private ListPageViewModel GetPage(int page)
        {
            return Pages.FirstOrDefault(k => k.Index == page);
        }

        public ICommand SwitchCommand
        {
            get
            {
                return new AsyncRelayCommand<object>(async obj =>
                {
                    if (obj is bool b)
                    {
                        if (CurrentPage == null) return;
                        var page = b ? GetPage(CurrentPage.Index + 1) : GetPage(CurrentPage.Index - 1);
                        if (page == null) return;
                        if (page.IsActivated)
                        {
                            return;
                        }

                        await PlayListQueryAsync(page.Index - 1, false);
                    }
                    else
                    {
                        var reqPage = (int)obj;
                        var page = GetPage(reqPage);
                        if (page == null) return;
                        if (page.IsActivated)
                        {
                            return;
                        }

                        await PlayListQueryAsync(reqPage - 1, false);
                    }
                });
            }
        }

        public ICommand SearchByConditionCommand
        {
            get
            {
                return new RelayCommand<object>(param =>
                {
                    WindowEx.GetCurrentFirst<MainWindow>()
                        .SwitchSearch
                        .CheckAndAction(page => ((SearchPage)page).Search((string)param));
                });
            }
        }

        public ICommand OpenSourceFolderCommand
        {
            get
            {
                return new AsyncRelayCommand<object>(async param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = await GetHighestSrBeatmapAsync(beatmap);
                    if (map == null) return;
                    var folderName = beatmap.GetFolder(out _, out _);
                    if (!Directory.Exists(folderName))
                    {
                        Notification.Push(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
                        return;
                    }

                    Process.Start(folderName);
                });
            }
        }

        public ICommand OpenScorePageCommand
        {
            get
            {
                return new AsyncRelayCommand<object>(async param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = await GetHighestSrBeatmapAsync(beatmap);
                    if (map == null) return;
                    Process.Start($"https://osu.ppy.sh/s/{map.BeatmapSetId}");
                });
            }
        }

        public ICommand SaveCollectionCommand
        {
            get
            {
                return new AsyncRelayCommand<object>(async param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var control = new DiffSelectControl(
                        await _playerData.GetBeatmapsFromFolderAsync(beatmap.GetIdentity().FolderName),
                        async (selected, arg) =>
                        {
                            arg.Handled = true;
                            var entry = (await _playerData.GetBeatmapsFromFolderAsync(selected.FolderName))
                                .FirstOrDefault(k => k.Version == selected.Version);
                            FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(entry),
                                DialogOptionFactory.SelectCollectionOptions);
                        });
                    FrontDialogOverlay.Default.ShowContent(control, DialogOptionFactory.DiffSelectOptions);
                });
            }
        }

        public ICommand ExportCommand
        {
            get
            {
                return new AsyncRelayCommand<object>(async param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = await GetHighestSrBeatmapAsync(beatmap);
                    if (map == null) return;
                    ExportPage.QueueEntry(map);
                });
            }
        }

        public ICommand DirectPlayCommand
        {
            get
            {
                return new AsyncRelayCommand<object>(async param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = await GetHighestSrBeatmapAsync(beatmap);
                    if (map == null) return;
                    var controller = Service.Get<ObservablePlayController>();
                    await controller.PlayNewAsync(map);
                });
            }
        }

        public ICommand PlayCommand
        {
            get
            {
                return new AsyncRelayCommand<object>(async param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var beatmaps = await _playerData.GetBeatmapsFromFolderAsync(beatmap.GetIdentity().FolderName);
                    var control = new DiffSelectControl(
                        beatmaps,
                        async (selected, arg) =>
                        {
                            var map = await _playerData.GetBeatmapByIdentifiableAsync(selected);
                            if (map == null) return;
                            var controller = Service.Get<ObservablePlayController>();
                            await controller.PlayNewAsync(map, true);
                        });
                    FrontDialogOverlay.Default.ShowContent(control, DialogOptionFactory.DiffSelectOptions);
                });
            }
        }

        private async Task<Beatmap> GetHighestSrBeatmapAsync(IMapIdentifiable beatmap)
        {
            if (beatmap == null) return null;
            var map = (await _playerData.GetBeatmapsFromFolderAsync(beatmap.FolderName)).GetHighestDiff();
            return map;
        }

        private CancellationTokenSource BeginQuery()
        {
            var next = new CancellationTokenSource();
            var previous = Interlocked.Exchange(ref _queryCancellation, next);
            if (previous != null)
            {
                previous.Cancel();
                previous.Dispose();
            }

            return next;
        }

        private void EndQuery(CancellationTokenSource cancellation)
        {
            if (Interlocked.CompareExchange(ref _queryCancellation, null, cancellation) == cancellation)
            {
                cancellation.Dispose();
            }
        }
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
}