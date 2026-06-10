using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Services;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Views.UserControls;

public partial class EditCollectionControl : UserControl
{
    private readonly IPlayerDataService? _playerData;
    private readonly Collection? _collection;
    private readonly EditCollectionPageViewModel? _viewModel;

    public EditCollectionControl()
    {
        if (App.Services != null)
        {
            _playerData = App.Services.GetService<IPlayerDataService>();
        }

        InitializeComponent();
        _viewModel = DataContext as EditCollectionPageViewModel;
    }

    public EditCollectionControl(Collection collection, IPlayerDataService playerData) : this()
    {
        _collection = collection;
        _playerData = playerData;
        if (_viewModel != null)
        {
            _viewModel.Name = collection.Name ?? string.Empty;
            _viewModel.Description = collection.Description ?? string.Empty;
            _viewModel.CoverPath = collection.ImagePath;
        }
    }

    private async void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        if (_collection == null || _viewModel == null || _playerData == null) return;
        _collection.Name = _viewModel.Name;
        _collection.Description = _viewModel.Description;
        _collection.ImagePath = _viewModel.CoverPath;
        await _playerData.TryUpdateCollectionAsync(_collection);
    }

    private void BtnChooseImg_Click(object? sender, RoutedEventArgs e)
    {
        AppNotificationService.Instance.Push("ui-btn-editCover");
    }
}
