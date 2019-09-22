using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Control.FrontDialog;
using Milky.OsuPlayer.ViewModels;

namespace Milky.OsuPlayer.Control
{
    /// <summary>
    /// EditCollectionControl.xaml 的交互逻辑
    /// </summary>
    public partial class EditCollectionControl : UserControl
    {
        private readonly Collection _collection;
        private AppDbOperator _appDbOperator = new AppDbOperator();
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

            _appDbOperator.UpdateCollection(_collection);
            FrontDialogOverlay.Default.RaiseOk();
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
