using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Converters;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.UiComponents.ContentDialogComponent;
using Milki.OsuPlayer.Wpf.Command;

namespace Milki.OsuPlayer.UserControls;

public class CurrentPlayControlVm : VmBase
{
    private readonly ExportService _exportService;

    private PlayItem _selectedMap;
    private List<PlayItem> _selectedMaps;
    private string _selectedPath;

    public CurrentPlayControlVm()
    {
        _exportService = ServiceProviders.Default.GetService<ExportService>();
        PlayerService = ServiceProviders.Default.GetService<PlayerService>();
        PlayListService = ServiceProviders.Default.GetService<PlayListService>();
    }

    public PlayerService PlayerService { get; }
    public PlayListService PlayListService { get; }

    public ICommand ClearPlayListCommand => new DelegateCommand(async param =>
    {
        PlayListService.SetPathList(Array.Empty<string>(), false);
        await using var applicationDbContext = ServiceProviders.GetApplicationDbContext();
        await applicationDbContext.RecreateCurrentPlayAsync(Array.Empty<PlayItem>());
    });

    public ICommand ExportCommand => new DelegateCommand(param =>
    {
        _exportService.QueueBeatmap(SelectedMap);
    });

    public ICommand OpenScorePageCommand => new DelegateCommand(param =>
    {
        ProcessUtils.StartWithShellExecute($"https://osu.ppy.sh/b/{SelectedMap.PlayItemDetail.BeatmapId}");
    });

    public ICommand OpenSourceFolderCommand => new DelegateCommand(param =>
    {
        ProcessUtils.StartWithShellExecute(PathUtils.GetFullPath(SelectedMap.StandardizedFolder,
            AppSettings.Default.GeneralSection.OsuSongDir));
    });

    public ICommand PlayCommand => new DelegateCommand(async param =>
    {
        await PlayerService.InitializeNewAsync(SelectedMap.StandardizedPath, true);
    });

    public ICommand RemoveCommand => new DelegateCommand(param =>
    {
        PlayListService.RemovePaths(SelectedMaps.Select(k => k.StandardizedPath));
    });

    public ICommand SaveAllPlayListCommand => new DelegateCommand(param =>
    {
        if (PlayListService.PathList.Count == 0) return;
        var playItems = PlayListService.PathList
            .Select(k => ((LoosePlayItem)PathToCurrentPlayConverter.Default?.GetLoosePlayItemByStandardizedPath(k, null))?.PlayItem)
            .ToArray();
        App.CurrentMainContentDialog.ShowContent(new SelectPlayListControl(playItems),
            DialogOptionFactory.SelectPlayListOptions);
    });

    public ICommand SavePlayListCommand => new DelegateCommand(param =>
    {
        App.CurrentMainContentDialog.ShowContent(new SelectPlayListControl(SelectedMap),
            DialogOptionFactory.SelectPlayListOptions);
    });

    public ICommand SearchCommand => new DelegateCommand(param =>
    {
    });

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

    public string SelectedPath
    {
        get => _selectedPath;
        set => this.RaiseAndSetIfChanged(ref _selectedPath, value);
    }
}

/// <summary>
///     PlayListControl.xaml 的交互逻辑
/// </summary>
public partial class CurrentPlayControl : UserControl
{
    public static readonly RoutedEvent CloseRequestedEvent =
        EventManager.RegisterRoutedEvent("CloseRequested",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(CurrentPlayControl));

    private readonly PlayerService _playerService;
    private readonly CurrentPlayControlVm _viewModel;

    private bool _signed = false;

    public CurrentPlayControl()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            _playerService = ServiceProviders.Default.GetService<PlayerService>();
            DataContext = _viewModel = new CurrentPlayControlVm();
        }

        InitializeComponent();
    }

    public event RoutedEventHandler CloseRequested
    {
        add => AddHandler(CloseRequestedEvent, value);
        remove => RemoveHandler(CloseRequestedEvent, value);
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