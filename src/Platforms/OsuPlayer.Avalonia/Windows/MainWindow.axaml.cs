using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OsuPlayer.Controls;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Lang;
using OsuPlayer.Localization;
using OsuPlayer.Playback;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Services;
using OsuPlayer.Shared;
using OsuPlayer.ViewModels;
using OsuPlayer.Views.Pages;
using OsuPlayer.Views.Pages.Settings;
using OsuPlayer.Views.UserControls;

namespace OsuPlayer.Windows;

public partial class MainWindow : Window
{
    private const string MainWindowDialogIdentifier = "MainWindowDialog";
    private readonly INavigationService _nav;
    private readonly IPlayerDataService? _playerData;
    private readonly ObservablePlayController? _controller;
    private readonly ILogger<MainWindow>? _logger;
    private ConfigWindow? _configWindow;
    private bool _disposed;
    private bool _forceExit;
    private bool _isClosingDialogOpen;
    private bool _playbackRestored;

    public MainWindow()
    {
        InitializeComponent();
        Closing += Window_Closing;
        _nav = null!;
    }

    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        PlayControllerVm playControllerVm,
        IPlayerDataService playerData,
        ObservablePlayController controller,
        ILogger<MainWindow> logger)
    {
        InitializeComponent();
        Closing += Window_Closing;
        DataContext = viewModel;
        _nav = navigationService;
        _playerData = playerData;
        _controller = controller;
        _logger = logger;
        _nav.Initialize(MainFrame);
        PlayBarController.DataContext = playControllerVm;
        PlayBarController.LikeClicked += Controller_LikeClicked;
        PlayBarController.ThumbClicked += Controller_ThumbClicked;
        Opened += OnOpened;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsNavigationCollapsed))
            {
                ApplyNavigationState(viewModel.IsNavigationCollapsed);
            }
        };
        ApplyNavigationState(viewModel.IsNavigationCollapsed);
        RegisterMessages();
        NavigateTo("Search");
    }

    private void RegisterMessages()
    {
        WeakReferenceMessenger.Default.Register<SearchRequestedMessage>(this, (_, message) =>
        {
            SearchNavButton.IsChecked = true;
            NavigateTo("Search", new SearchNavigationParameter(message.Value));
        });

        WeakReferenceMessenger.Default.Register<CollectionDeletedMessage>(this, async (_, _) =>
        {
            RecentNavButton.IsChecked = true;
            NavigateTo("Recent");
            await UpdateCollectionsAsync();
        });
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        await UpdateCollectionsAsync();
        await RestorePlaybackAsync();
    }

    private async Task RestorePlaybackAsync()
    {
        if (_playbackRestored || _controller == null || _playerData == null)
        {
            return;
        }

        _playbackRestored = true;

        var settings = AppSettings.Default;
        if (settings?.CurrentMap == null || !settings.Play.Memory)
        {
            return;
        }

        var currentMap = settings.CurrentMap.Value;
        if (string.IsNullOrEmpty(currentMap.FolderName) || string.IsNullOrEmpty(currentMap.Version))
        {
            return;
        }

        try
        {
            var savedEntries = settings.CurrentList?.Cast<IMapIdentifiable>() ??
                               Enumerable.Empty<IMapIdentifiable>();
            var entries = await _playerData.GetBeatmapsByIdentifiableAsync(
                savedEntries);
            await _controller.SetPlaylistAsync(entries, true, playInstantly: false, autoLoad: false);

            var play = settings.Play.AutoPlay;
            if (currentMap.IsMapTemporary())
            {
                await _controller.PlayNewAsync(currentMap.FolderName, play);
                return;
            }

            var current = await _playerData.GetBeatmapByIdentifiableAsync(currentMap);
            if (current != null)
            {
                await _controller.PlayNewAsync(current, play);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error while restoring previous playback.");
            AppNotificationService.Instance.Push("恢复上次播放失败");
        }
    }

    public async Task UpdateCollectionsAsync()
    {
        if (_playerData == null || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var list = await _playerData.GetCollectionsAsync();
        list.Reverse();
        viewModel.Collection = new ObservableCollection<Collection>(list);
    }

    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton clicked && clicked.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void OnCollectionNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string id })
        {
            NavigateTo("Collection", id);
        }
    }

    private async void BtnAddCollection_Click(object? sender, RoutedEventArgs e)
    {
        if (_playerData == null)
        {
            return;
        }

        var dialog = new AddCollectionWindow(_playerData);
        await dialog.ShowDialog(this);
        await UpdateCollectionsAsync();
    }

    private void OnCollapseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.CollapseCommand.Execute(null);
        }
    }

    private void NavigateTo(string tag, object? parameter = null)
    {
        _nav.NavigateTo(TagToPageType(tag), parameter);
    }

    private static Type TagToPageType(string tag) => tag switch
    {
        "Collection" => typeof(CollectionPage),
        "Search" => typeof(SearchPage),
        "Recent" => typeof(RecentPlayPage),
        "Export" => typeof(OsuPlayer.Views.Pages.ExportPage),
        "Find" => typeof(FindPage),
        "Settings" => typeof(InterfacePage),
        _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "Unknown navigation tag")
    };

    public void OpenSettingsWindow()
    {
        if (_configWindow == null)
        {
            _configWindow = App.Services.GetRequiredService<ConfigWindow>();
            _configWindow.Closed += (_, _) => _configWindow = null;
            _configWindow.Show(this);
            return;
        }

        _configWindow.Activate();
    }

    private void BtnSettings_Click(object? sender, RoutedEventArgs e)
    {
        OpenSettingsWindow();
    }

    private void BtnMini_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    public void ForceClose()
    {
        _forceExit = true;
        Close();
    }

    private async void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        var settings = AppSettings.Default;
        if (!_forceExit && settings?.General.ExitWhenClosed == null)
        {
            e.Cancel = true;
            await ShowClosingDialogAsync();
            return;
        }

        if (!_forceExit && settings?.General.ExitWhenClosed == false)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        if (_disposed)
        {
            return;
        }

        e.Cancel = true;
        await DisposeBeforeExitAsync();
        _forceExit = true;
        Close();
    }

    private async Task ShowClosingDialogAsync()
    {
        if (_isClosingDialogOpen)
        {
            return;
        }

        _isClosingDialogOpen = true;
        try
        {
            var closingControl = new ClosingControl();
            var dialog = new ContentDialog
            {
                Width = 280,
                Height = 180,
                Header = LocalizationService.Instance[SRKeys.Ui_Win_Closing],
                HeaderShowClose = true,
                FooterButtonStyle = FooterButtonStyle.Yes,
                FooterYesButtonText = LocalizationService.Instance[SRKeys.Ui_Ok],
                Content = closingControl
            };

            var result = await this.ShowContentDialog(dialog, MainWindowDialogIdentifier);
            if (result is true)
            {
                ApplyClosingChoice(closingControl);
            }
        }
        finally
        {
            _isClosingDialogOpen = false;
        }
    }

    private void ApplyClosingChoice(ClosingControl closingControl)
    {
        if (AppSettings.Default != null && closingControl.AsDefault.IsChecked == true)
        {
            AppSettings.Default.General.ExitWhenClosed = closingControl.RadioMinimum.IsChecked != true;
            AppSettings.SaveDefault();
        }

        if (closingControl.RadioMinimum.IsChecked == true)
        {
            HideToTray();
            return;
        }

        ForceClose();
    }

    private void HideToTray()
    {
        WindowState = WindowState.Minimized;
        Hide();
    }

    private async Task DisposeBeforeExitAsync()
    {
        _configWindow?.Close();

        if (_controller != null)
        {
            await _controller.DisposeAsync().ConfigureAwait(false);
        }

        _disposed = true;
    }

    private void Controller_ThumbClicked(object? sender, EventArgs e)
    {
        MainFrame.Content = null;
    }

    private async void Controller_LikeClicked(object? sender, EventArgs e)
    {
        if (PlayBarController.DataContext is PlayControllerVm vm && vm.Controller?.PlayList?.CurrentInfo != null && _playerData != null)
        {
            var detail = vm.Controller.PlayList.CurrentInfo.Beatmap;
            var entry = await _playerData.GetBeatmapByIdentifiableAsync(detail.GetIdentity());
            if (entry == null) return;

            var dialog = new SelectCollectionWindow(entry);
            await dialog.ShowDialog(this);
            await UpdateCollectionsAsync();
        }
    }

    private void ApplyNavigationState(bool collapsed)
    {
        // 宽度对齐 WPF: 展开 170px / 折叠 48px
        // Transitions 已在 AXAML 中声明 0.3s QuarticEaseInOut 动画
        SidebarHost.Width = collapsed ? 48 : 170;
    }
}
