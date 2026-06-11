using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.ViewModels;
using OsuPlayer.Views.Pages;
using OsuPlayer.Views.Pages.Settings;
using OsuPlayer.Views.UserControls;

namespace OsuPlayer.Windows;

public partial class MainWindow : Window
{
    private readonly INavigationService _nav;
    private readonly IPlayerDataService? _playerData;
    private ConfigWindow? _configWindow;

    public MainWindow()
    {
        InitializeComponent();
        _nav = null!;
    }

    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        PlayControllerVm playControllerVm,
        IPlayerDataService playerData)
    {
        InitializeComponent();
        DataContext = viewModel;
        _nav = navigationService;
        _playerData = playerData;
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
