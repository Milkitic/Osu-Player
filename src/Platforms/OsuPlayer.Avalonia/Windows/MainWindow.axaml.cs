using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.ViewModels;
using OsuPlayer.Views.Pages;
using OsuPlayer.Views.Pages.Settings;

namespace OsuPlayer.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainWindowViewModel>();
        var nav = App.Services.GetRequiredService<INavigationService>();
        nav.Initialize(MainFrame);

        // 默认显示 Collection 页面
        MainFrame.Content = App.Services.GetRequiredService<CollectionPage>();
    }

    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton tb && tb.Tag is string tag)
        {
            MainFrame.Content = tag switch
            {
                "Collection" => App.Services.GetRequiredService<CollectionPage>(),
                "Search" => App.Services.GetRequiredService<SearchPage>(),
                "Recent" => App.Services.GetRequiredService<RecentPlayPage>(),
                "Find" => App.Services.GetRequiredService<FindPage>(),
                "Settings" => App.Services.GetRequiredService<InterfacePage>(),
                _ => new TextBlock { Text = "Unknown" }
            };
        }
    }
}
