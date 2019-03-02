using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.EF.Model;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// EditCollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class EditCollectionPage : Page
    {
        private readonly MainWindow _mainWindow;
        private readonly Collection _collection;

        public EditCollectionPage(MainWindow mainWindow, Collection collection)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _collection = collection;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Dispose();
            _mainWindow.FramePop.Navigate(null);
        }

        private void Dispose()
        {
         
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel = (EditCollectionPageViewModel)DataContext;
            ViewModel.Name = _collection.Name;
            ViewModel.Description = _collection.Description;
            ViewModel.CoverPath = _collection.ImagePath;
        }

        public EditCollectionPageViewModel ViewModel { get; set; }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _collection.Name = ViewModel.Name;
            _collection.Description = ViewModel.Description;
            _collection.ImagePath = ViewModel.CoverPath;

            DbOperate.UpdateCollection(_collection);
            BtnClose_Click(sender, e);
        }

        private void BtnChooseImg_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fbd = new OpenFileDialog
            {
                Title = @"请选择一个图片",
                Filter = @"所有支持的图片类型|*.jpg;*.png;*.jpeg"
            };
            var result = fbd.ShowDialog();
            if (result == true)
            {
                ViewModel.CoverPath = fbd.FileName;
            }
        }
    }
}
