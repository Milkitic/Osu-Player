using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.ViewModels;
using OsuPlayer.Views.Pages;
using OsuPlayer.Views.Pages.Settings;
using OsuPlayer.Views.UserControls;

namespace OsuPlayer.Windows;

public partial class MainWindow : Window
{
    private readonly INavigationService _nav;

    public MainWindow()
    {
        InitializeComponent();
        _nav = null!;
    }

    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService, PlayControllerVm playControllerVm)
    {
        InitializeComponent();
        DataContext = viewModel;
        _nav = navigationService;
        _nav.Initialize(MainFrame);
        PlayBarController.DataContext = playControllerVm;
        NavigateTo("Search");
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

    private void NavigateTo(string tag)
    {
        _nav.NavigateTo(TagToPageType(tag));
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
}
