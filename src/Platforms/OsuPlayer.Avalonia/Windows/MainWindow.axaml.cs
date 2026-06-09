using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.ViewModels;
using OsuPlayer.ViewModels.Pages.Settings;
using OsuPlayer.Views.Pages;
using OsuPlayer.Views.Pages.Settings;
using OsuPlayerExport = OsuPlayer.Views.Pages.ExportPage;
using OsuPlayerExportVm = OsuPlayer.ViewModels.ExportPageViewModel;

namespace OsuPlayer.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var nav = App.Services.GetRequiredService<INavigationService>();
        nav.Initialize(MainFrame);
        NavigateTo("Search");
    }

    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton tb && tb.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void NavigateTo(string tag)
    {
        Control page;
        object? vm;

        switch (tag)
        {
            case "Collection":
                page = App.Services.GetRequiredService<CollectionPage>();
                vm = App.Services.GetRequiredService<CollectionPageViewModel>();
                break;
            case "Search":
                page = App.Services.GetRequiredService<SearchPage>();
                vm = App.Services.GetRequiredService<SearchPageViewModel>();
                break;
            case "Recent":
                page = App.Services.GetRequiredService<RecentPlayPage>();
                vm = App.Services.GetRequiredService<RecentPlayPageViewModel>();
                break;
            case "Export":
                page = App.Services.GetRequiredService<OsuPlayerExport>();
                vm = App.Services.GetRequiredService<OsuPlayerExportVm>();
                break;
            case "Find":
                page = App.Services.GetRequiredService<FindPage>();
                vm = null;
                break;
            case "Settings":
                page = App.Services.GetRequiredService<InterfacePage>();
                vm = App.Services.GetRequiredService<InterfacePageViewModel>();
                break;
            default:
                page = new TextBlock { Text = "Unknown" };
                vm = null;
                break;
        }

        page.DataContext = vm;
        MainFrame.Content = page;
    }

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