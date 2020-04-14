using Microsoft.Win32;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.UserControls
{
    /// <summary>
    /// EditCollectionControl.xaml 的交互逻辑
    /// </summary>
    public partial class EditCollectionControl : UserControl
    {
        private readonly Collection _collection;
        private static readonly SafeDbOperator SafeDbOperator = new SafeDbOperator();
        private EditCollectionPageViewModel _viewModel;

        public EditCollectionControl(Collection collection)
        {
            _collection = collection;
            InitializeComponent();
            _viewModel = (EditCollectionPageViewModel)DataContext;
            _viewModel.Name = _collection.Name;
            _viewModel.Description = _collection.Description;
            _viewModel.CoverPath = _collection.ImagePath;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _collection.Name = _viewModel.Name;
            _collection.Description = _viewModel.Description;
            _collection.ImagePath = _viewModel.CoverPath;

            if (SafeDbOperator.TryUpdateCollection(_collection))
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
}
