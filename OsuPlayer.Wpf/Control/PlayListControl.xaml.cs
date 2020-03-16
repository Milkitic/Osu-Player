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
using Milky.OsuPlayer.Control.FrontDialog;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Media.Audio;
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
        private Beatmap _selectedMap;
        private List<Beatmap> _selectedMaps;

        public ObservablePlayController Controller { get; } = Services.Get<ObservablePlayController>();

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
                    await Controller.PlayList.SetSongListAsync(Array.Empty<Beatmap>(), false);
                });
            }
        }

        public ICommand PlayCommand
        {
            get
            {
                return new DelegateCommand(async param =>
                {
                    await Controller.PlayNewAsync(SelectedMap);
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
                    Process.Start(SelectedMap.InOwnDb
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
                    FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(SelectedMap),
                        DialogOptionFactory.SelectCollectionOptions);
                    //var mw = WindowBase.GetCurrentFirst<MainWindow>();
                    //mw.FramePop.Navigate(new SelectCollectionPage(SelectedMap));
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
                    FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(Controller.PlayList.SongList),
                        DialogOptionFactory.SelectCollectionOptions);
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
                        Controller.PlayList.SongList.Remove(beatmap);
                    }
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

        public event RoutedEventHandler CloseRequested
        {
            add => AddHandler(CloseRequestedEvent, value);
            remove => RemoveHandler(CloseRequestedEvent, value);
        }

        private bool _signed;
        private readonly ObservablePlayController _controller = Services.Get<ObservablePlayController>();
        private PlayListControlVm _viewModel;

        public PlayListControl()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _viewModel = (PlayListControlVm)DataContext;
            _signed = false;
        }

        private void PlayListItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            _viewModel.PlayCommand?.Execute(null);
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
            _viewModel.SelectedMap = (Beatmap)e.AddedItems[e.AddedItems.Count - 1];
            var selected = e.AddedItems;
            _viewModel.SelectedMaps = selected.Cast<Beatmap>().ToList();
        }

        private void PlayList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (PlayList.SelectedItems.Count == 0)
                e.Handled = true;

            _viewModel.SelectedMap = (Beatmap)PlayList.SelectedItems[PlayList.SelectedItems.Count - 1];
            _viewModel.SelectedMaps = PlayList.SelectedItems.Cast<Beatmap>().ToList();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_signed)
            {
                _controller.LoadStarted += Controller_LoadStarted;
                _signed = true;
            }

            PlayList.SelectedItem = _controller.PlayList.CurrentInfo?.Beatmap;
            if (PlayList.SelectedItem != null)
            {
                PlayList.ScrollIntoView(PlayList.SelectedItem);
                ListViewItem item =
                    PlayList.ItemContainerGenerator.ContainerFromItem(PlayList.SelectedItem) as ListViewItem;
                item?.Focus();
            }
        }

        private void Controller_LoadStarted(BeatmapContext beatmapCtx, System.Threading.CancellationToken ct)
        {
            var info = _controller.PlayList.CurrentInfo?.Beatmap;
            if (info != null) PlayList.ScrollIntoView(info);
        }
    }
}
