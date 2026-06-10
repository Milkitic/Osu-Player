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
            SearchNavButton.IsChecked = true;
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
        if (sender is ToggleButton clicked && clicked.Tag is string tag)
        {
            foreach (var child in NavPanel.Children)
            {
                if (child is ToggleButton tb && tb != clicked)
                    tb.IsChecked = false;
            }
            NavigateTo(tag);
        }
    }

    private void OnCollectionNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id })
        {
            CollectionNavButton.IsChecked = true;
            NavigateTo("Collection", id);
        }
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

    private void BtnSettings_Click(object? sender, RoutedEventArgs e)
    {
        NavigateTo("Settings");
    }

    private void BtnMini_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnMaxRestore_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void BtnClose_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ApplyNavigationState(bool collapsed)
    {
        SidebarHost.Width = collapsed ? 64 : 188;
        WindowTitleText.IsVisible = !collapsed;
        CollapseButtonText.IsVisible = !collapsed;
        LibraryHeader.IsVisible = !collapsed;
        MineHeader.IsVisible = !collapsed;
        CollectionHeader.IsVisible = !collapsed;
        SearchNavText.IsVisible = !collapsed;
        RecentNavText.IsVisible = !collapsed;
        ExportNavText.IsVisible = !collapsed;
        CollectionNavText.IsVisible = !collapsed;
        FindNavText.IsVisible = !collapsed;
        CollectionList.IsVisible = !collapsed;

        if (AppSettings.Default != null)
        {
            AppSettings.Default.General.IsNavigationCollapsed = collapsed;
            AppSettings.SaveDefault();
        }
    }
}
