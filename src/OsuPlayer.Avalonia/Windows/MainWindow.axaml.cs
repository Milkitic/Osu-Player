using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Primitives;  // ToggleButton
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Avalonia.Interaction;
using OsuPlayer.Avalonia.ViewModels;

namespace OsuPlayer.Avalonia.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainWindowViewModel>();
        var nav = App.Services.GetRequiredService<INavigationService>();
        nav.Initialize(MainFrame);
    }

    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton tb && tb.Tag is string tag)
        {
            MainFrame.Content = tag switch
            {
                "Collection" => new TextBlock { Text = "Collection Page (Avalonia)", Foreground = Brushes.White, FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                "Search" => new TextBlock { Text = "Search Page", Foreground = Brushes.White, FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                "Recent" => new TextBlock { Text = "Recent Page", Foreground = Brushes.White, FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                "Find" => new TextBlock { Text = "Find Page", Foreground = Brushes.White, FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                "Settings" => new TextBlock { Text = "Settings Page", Foreground = Brushes.White, FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                _ => new TextBlock { Text = "Unknown" }
            };
        }
    }
}
