using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.ViewModels;

namespace Milki.OsuPlayer.UserControls;

/// <summary>
/// EditCollectionControl.xaml 的交互逻辑
/// </summary>
public partial class EditCollectionControl : UserControl
{
    private readonly PlayList _collection;
    private readonly EditCollectionPageViewModel _viewModel;

    public EditCollectionControl(PlayList collection)
    {
        _collection = collection;
        InitializeComponent();
        DataContext = _viewModel = new EditCollectionPageViewModel();
        _viewModel.Name = _collection.Name;
        _viewModel.Description = _collection.Description;
        _viewModel.CoverPath = _collection.ImagePath;
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        _collection.Name = _viewModel.Name;
        _collection.Description = _viewModel.Description;
        _collection.ImagePath = _viewModel.CoverPath;

        var appDbContext = ServiceProviders.GetApplicationDbContext();
        await appDbContext.AddOrUpdatePlayListAsync(_collection);
        App.Current.MainWindow.ContentDialog.RaiseOk();
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