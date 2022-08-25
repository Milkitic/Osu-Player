using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Pages;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.UiComponents.ContentDialogComponent;
using Milki.OsuPlayer.Wpf.Command;

namespace Milki.OsuPlayer.UserControls;

public class CurrentPlayControlVm : VmBase
{
    private readonly PlayerService _playerService;
    private readonly PlayListService _playListService;

    private PlayItem _selectedMap;
    private List<PlayItem> _selectedMaps;

    public CurrentPlayControlVm()
    {
        _playerService = ServiceProviders.Default.GetService<PlayerService>();
        _playListService = ServiceProviders.Default.GetService<PlayListService>();
    }

    public PlayItem SelectedMap
    {
        get => _selectedMap;
        set => this.RaiseAndSetIfChanged(ref _selectedMap, value);
    }

    public List<PlayItem> SelectedMaps
    {
        get => _selectedMaps;
        set => this.RaiseAndSetIfChanged(ref _selectedMaps, value);
    }

    public ICommand ClearPlayListCommand
    {
        get
        {
            return new DelegateCommand(param =>
            {
                _playListService.SetPathList(Array.Empty<string>(), false);
            });
        }
    }

    public ICommand PlayCommand
    {
        get
        {
            return new DelegateCommand(async param =>
            {
                await _playerService.InitializeNewAsync(SelectedMap.StandardizedPath, true);
            });
        }
    }

    public ICommand SearchCommand
    {
        get
        {
            return new DelegateCommand(param =>
            {
                //var mw = WindowEx.GetCurrentFirst<MainWindow>();
                //var s = (string)param;
                //switch (s)
                //{
                //    case "0":
                //        mw.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(SelectedMap.PreferredTitle));
                //        break;
                //    case "1":
                //        mw.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(SelectedMap.PreferredArtist));
                //        break;
                //    case "2":
                //        mw.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(SelectedMap.SongSource));
                //        break;
                //    case "3":
                //        mw.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(SelectedMap.Creator));
                //        break;
                //}
            });
        }
    }

    public ICommand OpenSourceFolderCommand
    {
        get
        {
            return new DelegateCommand(param =>
            {
                Process.Start(PathUtils.GetFullPath(SelectedMap.StandardizedFolder,
                    AppSettings.Default.GeneralSection.OsuSongDir));
            });
        }
    }

    public ICommand OpenScorePageCommand
    {
        get
        {
            return new DelegateCommand(param =>
            {
                Process.Start($"https://osu.ppy.sh/b/{SelectedMap.PlayItemDetail.BeatmapId}");
            });
        }
    }

    public ICommand SavePlayListCommand
    {
        get
        {
            return new DelegateCommand(param =>
            {
                App.CurrentMainContentDialog.ShowContent(new SelectPlayListControl(SelectedMap),
                    DialogOptionFactory.SelectPlayListOptions);
                //var mw = WindowEx.GetCurrentFirst<MainWindow>();
                //mw.FramePop.Navigate(new SelectPlayListPage(SelectedMap));
            });
        }
    }

    public ICommand SaveAllPlayListCommand
    {
        get
        {
            return new DelegateCommand(param =>
            {
                if (_playListService.PathList.Count == 0) return;
                //FrontDialogOverlay.Default.ShowContent(new SelectPlayListControl(Controller.PlayList.SongList),
                //    DialogOptionFactory.SelectPlayListOptions);
            });
        }
    }

    public ICommand ExportCommand
    {
        get
        {
            return new DelegateCommand(param =>
            {
                ExportPage.QueueBeatmap(SelectedMap);
            });
        }
    }

    public ICommand RemoveCommand
    {
        get
        {
            return new DelegateCommand(param =>
            {
                _playListService.RemovePaths(SelectedMaps.Select(k => k.StandardizedPath));
            });
        }
    }
}

/// <summary>
/// PlayListControl.xaml 的交互逻辑
/// </summary>
public partial class CurrentPlayControl : UserControl
{
    public static readonly RoutedEvent CloseRequestedEvent =
        EventManager.RegisterRoutedEvent("CloseRequested",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(CurrentPlayControl));

    public event RoutedEventHandler CloseRequested
    {
        add => AddHandler(CloseRequestedEvent, value);
        remove => RemoveHandler(CloseRequestedEvent, value);
    }

    private bool _signed = false;
    private readonly PlayerService _playerService;
    private readonly CurrentPlayControlVm _viewModel;

    public CurrentPlayControl()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            _playerService = ServiceProviders.Default.GetService<PlayerService>();
            DataContext = _viewModel = new CurrentPlayControlVm();
        }

        InitializeComponent();
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
        _viewModel.SelectedMap = (PlayItem)e.AddedItems[e.AddedItems.Count - 1];
        var selected = e.AddedItems;
        _viewModel.SelectedMaps = selected.Cast<PlayItem>().ToList();
    }

    private void PlayList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (PlayList.SelectedItems.Count == 0)
        {
            e.Handled = true;
            return;
        }

        _viewModel.SelectedMap = (PlayItem)PlayList.SelectedItems[PlayList.SelectedItems.Count - 1];
        _viewModel.SelectedMaps = PlayList.SelectedItems.Cast<PlayItem>().ToList();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_signed)
        {
            if (_playerService != null)
                _playerService.LoadStarted += PlayerService_LoadStarted;
            _signed = true;
        }

        if (_playerService != null)
        {
            PlayList.SelectedItem = _playerService.LastLoadContext?.PlayItem;
        }

        if (PlayList.SelectedItem != null)
        {
            PlayList.ScrollIntoView(PlayList.SelectedItem);
            ListViewItem item =
                PlayList.ItemContainerGenerator.ContainerFromItem(PlayList.SelectedItem) as ListViewItem;
            item?.Focus();
        }
    }

    private ValueTask PlayerService_LoadStarted(PlayerService.PlayItemLoadContext arg)
    {
        var playItem = arg.PlayItem;
        if (playItem != null) PlayList.ScrollIntoView(playItem);
        return ValueTask.CompletedTask;
    }
}