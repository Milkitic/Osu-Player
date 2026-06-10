using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Views.Pages.Settings;

namespace OsuPlayer.Windows;

public partial class ConfigWindow : Window
{
    private readonly INavigationService _navigationService;

    public ConfigWindow()
    {
        InitializeComponent();
        _navigationService = null!;
    }

    public ConfigWindow(INavigationService navigationService)
    {
        InitializeComponent();
        _navigationService = navigationService;
        _navigationService.Initialize(MainFrame);
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        NavigateTo("General");
    }

    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton clicked && clicked.Tag is string tag)
        {
            foreach (var candidate in new[] { GeneralButton, PlayButton, InterfaceButton, HotKeyButton, LyricButton, ExportButton, AboutButton })
            {
                if (candidate != clicked)
                {
                    candidate.IsChecked = false;
                }
            }

            NavigateTo(tag);
        }
    }

    private void NavigateTo(string tag)
    {
        _navigationService.NavigateTo(tag switch
        {
            "General" => typeof(GeneralPage),
            "Play" => typeof(PlayPage),
            "Interface" => typeof(InterfacePage),
            "HotKey" => typeof(HotKeyPage),
            "Lyric" => typeof(LyricPage),
            "Export" => typeof(ExportPage),
            "About" => typeof(AboutPage),
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "Unknown settings tag")
        });
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
