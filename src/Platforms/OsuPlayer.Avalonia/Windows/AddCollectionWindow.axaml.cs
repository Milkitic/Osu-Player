using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OsuPlayer.Core.Services;

namespace OsuPlayer.Windows;

public partial class AddCollectionWindow : Window
{
    private readonly IPlayerDataService? _playerData;

    public AddCollectionWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    public AddCollectionWindow(IPlayerDataService playerData) : this()
    {
        _playerData = playerData;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        ContentHost.FocusCollectionName();
    }

    private async void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (_playerData == null)
        {
            return;
        }

        var collectionName = ContentHost.CollectionNameValue;
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            ContentHost.FocusCollectionName();
            return;
        }

        if (await _playerData.TryAddCollectionAsync(collectionName, false))
        {
            Close();
            return;
        }

        ContentHost.FocusCollectionName();
    }
}
