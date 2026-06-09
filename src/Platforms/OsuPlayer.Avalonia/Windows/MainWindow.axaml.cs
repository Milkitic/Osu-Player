using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.ViewModels;
using OsuPlayer.ViewModels.Pages.Settings;
using OsuPlayer.Views.Pages;
using OsuPlayer.Views.Pages.Settings;

namespace OsuPlayer.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var nav = App.Services.GetRequiredService<INavigationService>();
        nav.Initialize(MainFrame);
        NavigateTo("Collection");
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
        var page = tag switch
        {
            "Collection" => (Control)App.Services.GetRequiredService<CollectionPage>(),
            "Search" => App.Services.GetRequiredService<SearchPage>(),
            "Recent" => App.Services.GetRequiredService<RecentPlayPage>(),
            "Find" => App.Services.GetRequiredService<FindPage>(),
            "Settings" => App.Services.GetRequiredService<InterfacePage>(),
            _ => new TextBlock { Text = "Unknown" }
        };

        page.DataContext = tag switch
        {
            "Collection" => App.Services.GetRequiredService<CollectionPageViewModel>(),
            "Search" => App.Services.GetRequiredService<SearchPageViewModel>(),
            "Recent" => App.Services.GetRequiredService<RecentPlayPageViewModel>(),
            "Settings" => App.Services.GetRequiredService<InterfacePageViewModel>(),
            _ => null
        };

        MainFrame.Content = page;
    }
}
