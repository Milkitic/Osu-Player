using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.UiComponents.FrontDialogComponent;
using OsuPlayer.ViewModels;

namespace OsuPlayer.UserControls;

/// <summary>
/// EditCollectionControl.xaml 的交互逻辑
/// </summary>
public partial class EditCollectionControl : UserControl
{
    private readonly IPlayerDataService _playerData;
    private readonly Collection _collection;
    private EditCollectionPageViewModel _viewModel;

    public EditCollectionControl(Collection collection, IPlayerDataService playerData)
    {
        _collection = collection;
        _playerData = playerData;

        InitializeComponent();
        _viewModel = (EditCollectionPageViewModel)DataContext;
        _viewModel.Name = _collection.Name;
        _viewModel.Description = _collection.Description;
        _viewModel.CoverPath = _collection.ImagePath;
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        _collection.Name = _viewModel.Name;
        _collection.Description = _viewModel.Description;
        _collection.ImagePath = _viewModel.CoverPath;

        if (await _playerData.TryUpdateCollectionAsync(_collection))
        {
            FrontDialogOverlay.Default.RaiseOk();
        }
    }

    private void BtnChooseImg_Click(object sender, RoutedEventArgs e)
    {
        var fbd = new OpenFileDialog
        {
            Title = @"请选择一个图片",
            Filter = @"所有支持的图片类型|*.jpg;*.png;*.jpeg"
        };
        var result = fbd.ShowDialog();
        if (result == true)
        {
            _viewModel.CoverPath = fbd.FileName;
        }
    }
}
