using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Services;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Views.UserControls;

public partial class SelectCollectionControl : UserControl
{
    private readonly IPlayerDataService? _playerData;
    private readonly SelectCollectionPageViewModel? _viewModel;

    public SelectCollectionControl()
    {
        if (App.Services != null)
        {
            _playerData = App.Services.GetService<IPlayerDataService>();
        }

        InitializeComponent();
        _viewModel = DataContext as SelectCollectionPageViewModel;
        _ = RefreshListAsync();
    }

    public SelectCollectionControl(Beatmap entry) : this(new List<Beatmap> { entry })
    {
    }

    public SelectCollectionControl(IList<Beatmap> entries) : this()
    {
        if (_viewModel != null) _viewModel.Entries = entries;
    }

    private void BtnAddCollection_Click(object? sender, RoutedEventArgs e)
    {
        AppNotificationService.Instance.Push("ui-addNewCollection");
    }

    private void BtnClose_Click(object? sender, RoutedEventArgs e)
    {
    }

    private async Task RefreshListAsync()
    {
        if (_viewModel == null || _playerData == null) return;
        var cols = await _playerData.GetCollectionsAsync();
        _viewModel.Collections = new ObservableCollection<Collection>(cols.OrderByDescending(k => k.CreateTime));
    }
}
