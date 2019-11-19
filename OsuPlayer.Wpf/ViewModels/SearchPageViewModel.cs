using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Metadata;
using Milky.WpfApi;
using Milky.WpfApi.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Data.EF.Model;
using BeatmapDbOperator = Milky.OsuPlayer.Common.Data.EF.BeatmapDbOperator;
using System.Windows.Markup;
using System.Xaml;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Control.FrontDialog;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Windows;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.ViewModels
{
    public class SearchPageViewModel : ViewModelBase
    {
        private BeatmapDbOperator _beatmapDbOperator = new BeatmapDbOperator();

        private const int MaxListCount = 100;
        private List<BeatmapDataModel> _searchedMaps;
        private List<BeatmapDataModel> _displayedMaps;

        private List<ListPageViewModel> _pages;
        private ListPageViewModel _lastPage;
        private ListPageViewModel _firstPage;
        private ListPageViewModel _currentPage;
        private string _searchText;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public List<Beatmap> SearchedDbMaps
        {
            get => _searchedDbMaps;
            set
            {
                _searchedDbMaps = value;
                OnPropertyChanged();
            }
        }

        public List<BeatmapDataModel> SearchedMaps
        {
            get => _searchedMaps;
            private set
            {
                _searchedMaps = value;
                OnPropertyChanged();
            }
        }
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
        public ListPageViewModel LastPage
        {
            get => _lastPage;
            private set
            {
                _lastPage = value;
                OnPropertyChanged();
            }
        }

        public ListPageViewModel FirstPage
        {
            get => _firstPage;
            private set
            {
                _firstPage = value;
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

        private readonly Stopwatch _querySw = new Stopwatch();
        private bool _isQuerying;
        private List<Beatmap> _searchedDbMaps;
        private static readonly object QueryLock = new object();

        public async Task PlayListQueryAsync(int startIndex = 0)
        {
            //if (Services.Get<OsuDbInst>().Beatmaps == null)
            //    return;

            //SortEnum sortEnum = (SortEnum)cbSortType.SelectedItem;
            var sortMode = SortMode.Artist;
            _querySw.Restart();

            lock (QueryLock)
            {
                if (_isQuerying)
                    return;
                _isQuerying = true;
            }

            await Task.Run(() =>
            {
                while (_querySw.ElapsedMilliseconds < 300)
                    Thread.Sleep(1);
                _querySw.Stop();

                SearchedDbMaps = _beatmapDbOperator
                    .SearchBeatmapByOptions(SearchText, SortMode.Artist, startIndex, int.MaxValue);
                List<BeatmapDataModel> sorted = SearchedDbMaps
                    .ToDataModelList(true);


                SearchedMaps = sorted;
                SetPage(SearchedMaps.Count(), 0);
            });

            lock (QueryLock)
            {
                _isQuerying = false;
            }
        }

        private void SetPage(int totalCount, int nowIndex)
        {
            totalCount = (int)Math.Ceiling(totalCount / (float)MaxListCount);
            int count, startIndex;
            if (totalCount > 10)
            {
                if (nowIndex > 5)
                {
                    FirstPage = new ListPageViewModel(1);
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
            DisplayedMaps = SearchedMaps.Skip(nowIndex * MaxListCount).Take(MaxListCount).ToList();
        }

        private ListPageViewModel GetPage(int page)
        {
            return Pages.FirstOrDefault(k => k.Index == page);
        }

        public ICommand SwitchCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    if (obj is bool b)
                    {
                        var page = b ? GetPage(CurrentPage.Index + 1) : GetPage(CurrentPage.Index - 1);
                        if (page == null) return;
                        if (page.IsActivated)
                        {
                            return;
                        }

                        SetPage(SearchedMaps.Count(), page.Index - 1);
                    }
                    else
                    {
                        var reqPage = (int)obj;
                        var page = GetPage(reqPage);
                        if (page.IsActivated)
                        {
                            return;
                        }

                        SetPage(SearchedMaps.Count(), reqPage - 1);
                    }
                });
            }
        }

        public ICommand SearchByConditionCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    WindowBase.GetCurrentFirst<MainWindow>()
                        .SwitchSearch
                        .CheckAndAction(page => ((SearchPage)page).Search((string)param));
                });
            }
        }

        public ICommand OpenSourceFolderCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = GetHighestSrBeatmap(beatmap);
                    if (map == null) return;
                    var fileName = Path.Combine(Domain.OsuSongPath, map.FolderName);
                    if (!File.Exists(fileName))
                    {
                        Notification.Show(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
                        return;
                    }

                    Process.Start(fileName);
                });
            }
        }

        public ICommand OpenScorePageCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = GetHighestSrBeatmap(beatmap);
                    if (map == null) return;
                    Process.Start($"https://osu.ppy.sh/s/{map.BeatmapSetId}");
                });
            }
        }

        public ICommand SaveCollectionCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var control = new DiffSelectControl(
                        _beatmapDbOperator.GetBeatmapsFromFolder(beatmap.GetIdentity().FolderName),
                        selected =>
                        {
                            var entry = _beatmapDbOperator.GetBeatmapsFromFolder(selected.FolderName)
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
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = GetHighestSrBeatmap(beatmap);
                    if (map == null) return;
                    ExportPage.QueueEntry(map);
                });
            }
        }

        public ICommand DirectPlayCommand
        {
            get
            {
                return new DelegateCommand(async param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = GetHighestSrBeatmap(beatmap);
                    if (map == null) return;
                    await PlayController.Default.PlayNewFile(map);
                    await Services.Get<PlayerList>()
                        .RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.RecentList);
                });
            }
        }

        public ICommand PlayCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var beatmaps = _beatmapDbOperator.GetBeatmapsFromFolder(beatmap.GetIdentity().FolderName);
                    var control = new DiffSelectControl(
                        beatmaps,
                        async selected =>
                        {
                            var map = _beatmapDbOperator.GetBeatmapByIdentifiable(selected);
                            await PlayController.Default.PlayNewFile(map);
                            await Services.Get<PlayerList>()
                                .RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.RecentList);
                        });
                    FrontDialogOverlay.Default.ShowContent(control, DialogOptionFactory.DiffSelectOptions);
                });
            }
        }

        private Beatmap GetHighestSrBeatmap(IMapIdentifiable beatmap)
        {
            var map = _beatmapDbOperator.GetBeatmapsFromFolder(beatmap.FolderName).GetHighestDiff();
            return map;
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
