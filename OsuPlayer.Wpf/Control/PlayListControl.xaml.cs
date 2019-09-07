using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.Windows;
using Milky.WpfApi;
using Milky.WpfApi.Commands;
using OSharp.Beatmap;
using Path = System.IO.Path;

namespace Milky.OsuPlayer.Control
{
    public class PlayListControlVm : ViewModelBase
    {
        private PlayerList _playList;
        private Beatmap _selectedMap;
        private List<Beatmap> _selectedMaps;

        public PlayerList PlayList
        {
            get => _playList;
            set
            {
                _playList = value;
                OnPropertyChanged();
            }
        }

        public Beatmap SelectedMap
        {
            get => _selectedMap;
            set
            {
                _selectedMap = value;
                OnPropertyChanged();
            }
        }

        public List<Beatmap> SelectedMaps
        {
            get => _selectedMaps;
            set
            {
                _selectedMaps = value;
                OnPropertyChanged();
            }
        }
        public ICommand ClearPlayListCommand
        {
            get
            {
                return new DelegateCommand(async param =>
                {
                    PlayList.Entries.Clear();
                    await PlayList.RefreshPlayListAsync(PlayerList.FreshType.IndexOnly);
                });
            }
        }

        public ICommand PlayCommand
        {
            get
            {
                return new DelegateCommand(async param =>
                {
                    await PlayController.Default.PlayNewFile(SelectedMap);
                    PlayList.Pointer = PlayList.Indexes.FirstOrDefault(k => PlayList.Entries[k] == SelectedMap);
                });
            }
        }

        public ICommand SearchCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var mw = WindowBase.GetCurrentFirst<MainWindow>();
                    var s = (string)param;
                    switch (s)
                    {
                        case "0":
                            mw.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(SelectedMap.AutoTitle));
                            break;
                        case "1":
                            mw.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(SelectedMap.AutoArtist));
                            break;
                        case "2":
                            mw.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(SelectedMap.SongSource));
                            break;
                        case "3":
                            mw.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(SelectedMap.Creator));
                            break;
                    }
                });
            }
        }

        public ICommand OpenSourceFolderCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    Process.Start(SelectedMap.InOwnFolder
                        ? Path.Combine(Domain.CustomSongPath, SelectedMap.FolderName)
                        : Path.Combine(Domain.OsuSongPath, SelectedMap.FolderName));
                });
            }
        }

        public ICommand OpenScorePageCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    Process.Start($"https://osu.ppy.sh/b/{SelectedMap.BeatmapId}");
                });
            }
        }

        public ICommand SaveCollectionCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var mw = WindowBase.GetCurrentFirst<MainWindow>();
                    mw.FramePop.Navigate(new SelectCollectionPage(SelectedMap));
                });
            }
        }

        public object SaveAllCollectionCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var mw = WindowBase.GetCurrentFirst<MainWindow>();
                    mw.FramePop.Navigate(new SelectCollectionPage(PlayList.Entries));
                });
            }
        }

        public ICommand ExportCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    ExportPage.QueueEntry(SelectedMap);
                });
            }
        }

        public ICommand RemoveCommand
        {
            get
            {
                return new DelegateCommand(async param =>
                {
                    foreach (var beatmap in SelectedMaps)
                    {
                        PlayList.Entries.Remove(beatmap);
                    }
                    await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.IndexOnly);
                });
            }
        }
    }

    /// <summary>
    /// PlayListControl.xaml 的交互逻辑
    /// </summary>
    public partial class PlayListControl : UserControl
    {
        public static readonly RoutedEvent CloseRequestedEvent =
            EventManager.RegisterRoutedEvent("CloseRequested",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(PlayListControl));

        private bool _signed;

        // .NET event wrapper
        public event RoutedEventHandler CloseRequested
        {
            add => AddHandler(CloseRequestedEvent, value);
            remove => RemoveHandler(CloseRequestedEvent, value);
        }


        public PlayListControl()
        {
            InitializeComponent();
        }

        public PlayListControlVm ViewModel { get; set; }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            ViewModel = (PlayListControlVm)this.DataContext;
            ViewModel.PlayList = Services.Get<PlayerList>();
            var helper = new GridViewHelper(PlayList);
            helper.OnMouseDoubleClick(PlayList_GridViewRow_DoubleClick);
            _signed = false;
        }

        private void PlayList_GridViewRow_DoubleClick(object sender, RoutedEventArgs e)
        {
            ViewModel.PlayCommand?.Execute(null);
        }

        private void Controller_OnNewFileLoaded(object sender, HandledEventArgs e)
        {
            var info = Services.Get<PlayerList>().CurrentInfo;
            if (info == null)
                return;
            var identity = info.MapInfo.GetIdentity();
        }

        private void PlayList_MouseMove(object sender, MouseEventArgs e)
        {
            //var item = Mouse.DirectlyOver;
            //if (item is Border b && b.Child is GridViewRowPresenter pre)
            //{
            //    var beatmap
            //}
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseRequestedEvent, this));
            //RaiseEvent(e);
        }

        private void PlayList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;
            ViewModel.SelectedMap = (Beatmap)e.AddedItems[e.AddedItems.Count - 1];
            var selected = e.AddedItems;
            ViewModel.SelectedMaps = selected.Cast<Beatmap>().ToList();
        }

        private void PlayList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (PlayList.SelectedItems.Count == 0)
                e.Handled = true;

            ViewModel.SelectedMap = (Beatmap)PlayList.SelectedItems[PlayList.SelectedItems.Count - 1];
            ViewModel.SelectedMaps = PlayList.SelectedItems.Cast<Beatmap>().ToList();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_signed && PlayController.Default != null)
            {
                PlayController.Default.OnNewFileLoaded += Controller_OnNewFileLoaded;
                _signed = true;
            }
        }
    }
}
