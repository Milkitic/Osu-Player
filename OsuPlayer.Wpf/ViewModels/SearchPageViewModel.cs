using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Metadata;
using Milky.WpfApi;
using Milky.WpfApi.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Instances;

namespace Milky.OsuPlayer.ViewModels
{
    public class SearchPageViewModel : ViewModelBase
    {
        private const int MaxListCount = 100;
        private IEnumerable<BeatmapDataModel> _searchedMaps;
        private IEnumerable<BeatmapDataModel> _displayedMaps;

        private IEnumerable<ListPage> _pages;
        private ListPage _lastPage;
        private ListPage _firstPage;
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

        public IEnumerable<BeatmapDataModel> SearchedMaps
        {
            get => _searchedMaps;
            private set
            {
                _searchedMaps = value;
                OnPropertyChanged();
            }
        }
        public IEnumerable<BeatmapDataModel> DisplayedMaps
        {
            get => _displayedMaps;
            private set
            {
                _displayedMaps = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<ListPage> Pages
        {
            get => _pages;
            private set
            {
                _pages = value;
                OnPropertyChanged();
            }
        }
        public ListPage LastPage
        {
            get => _lastPage;
            private set
            {
                _lastPage = value;
                OnPropertyChanged();
            }
        }

        public ListPage FirstPage
        {
            get => _firstPage;
            private set
            {
                _firstPage = value;
                OnPropertyChanged();
            }
        }


        private readonly Stopwatch _querySw = new Stopwatch();
        private bool _isQuerying;
        private static readonly object QueryLock = new object();

        public async Task PlayListQueryAsync()
        {
            if (InstanceManage.GetInstance<OsuDbInst>().Beatmaps == null)
                return;

            //SortEnum sortEnum = (SortEnum)cbSortType.SelectedItem;
            SortMode sortMode = SortMode.Artist;
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

                var sorted = string.IsNullOrWhiteSpace(SearchText)
                    ? InstanceManage.GetInstance<OsuDbInst>().Beatmaps.SortBy(sortMode).ToDataModels(true).ToList()
                    : InstanceManage.GetInstance<OsuDbInst>().Beatmaps.FilterByKeyword(SearchText).SortBy(sortMode).ToDataModels(true);

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
                    FirstPage = new ListPage(1);
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

            var pages = new List<ListPage>(totalCount);
            for (int i = startIndex; i < startIndex + count; i++)
            {
                pages.Add(new ListPage(i + 1));
            }

            Pages = pages;
            ListPage page = GetPage(nowIndex + 1);

            if (page != null) page.IsActivated = true;
            DisplayedMaps = SearchedMaps.Skip(nowIndex * MaxListCount).Take(MaxListCount);
        }

        private ListPage GetPage(int page)
        {
            return Pages.FirstOrDefault(k => k.Index == page);
        }

        public ICommand SwitchCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    var reqPage = (int)obj;
                    var page = GetPage(reqPage);
                    if (page.IsActivated)
                    {
                        return;
                    }

                    SetPage(SearchedMaps.Count(), reqPage - 1);
                });
            }
        }
    }

    public class ListPage : ViewModelBase
    {
        public ListPage(int index)
        {
            Index = index;
        }

        public int Index { get; set; }
        public bool IsActivated { get; set; }
    }
}
