using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Avalonia.ViewModels;
using OsuPlayer.Avalonia.Views.Pages.Settings;
using OsuPlayer.Presentation.Interaction;

namespace OsuPlayer.Avalonia.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainWindowViewModel>();
        var nav = App.Services.GetRequiredService<INavigationService>();
        nav.Initialize(MainFrame);

        // 默认显示 Collection 页面
        MainFrame.Content = App.Services.GetRequiredService<OsuPlayer.Avalonia.Views.Pages.CollectionPage>();
    }

    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton tb && tb.Tag is string tag)
        {
            MainFrame.Content = tag switch
            {
                "Collection" => App.Services.GetRequiredService<OsuPlayer.Avalonia.Views.Pages.CollectionPage>(),
                "Search" => App.Services.GetRequiredService<OsuPlayer.Avalonia.Views.Pages.SearchPage>(),
                "Recent" => App.Services.GetRequiredService<OsuPlayer.Avalonia.Views.Pages.RecentPlayPage>(),
                "Find" => App.Services.GetRequiredService<OsuPlayer.Avalonia.Views.Pages.FindPage>(),
                "Settings" => App.Services.GetRequiredService<InterfacePage>(),
                _ => new TextBlock { Text = "Unknown" }
            };
        }
    }
}
