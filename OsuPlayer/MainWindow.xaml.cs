using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Storage;
using Coosu.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using OsuPlayer.Data;
using OsuPlayer.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
//https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.symbol?view=winrt-22000

namespace OsuPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly AppWindow _appWindow;

        public MainWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            _appWindow = this.GetAppWindow();

            SetInitialRect();
        }

        private void SetInitialRect()
        {
            var area = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
            var width = 1185;
            var height = 895;
            var left = area.WorkArea.X + area.WorkArea.Width / 2 - width / 2;
            var top = area.WorkArea.Y + area.WorkArea.Height / 2 - height / 2;
            _appWindow.MoveAndResize(new RectInt32(left, top, width, height));
        }

        private void myButton_Click(object sender, RoutedEventArgs e)
        {
            //myButton.Content = "Clicked";
        }

        private async void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            await using var appDbContext = new ApplicationDbContext();
            await appDbContext.Database.MigrateAsync();

            var settingsItem = (NavigationViewItem)NavigationView.SettingsItem;
            settingsItem.Foreground = new SolidColorBrush(Colors.White);
            //var reader = new OsuDbReader(@"E:\Games\osu!\osu!.db");
            //var beatmaps = reader.EnumerateDbModels();
            //var syncer = new BeatmapSyncService(appDbContext);
            //await syncer.SynchronizeManaged(beatmaps);
        }

        private void NavigationView_OnSelectionChanged(NavigationView sender,
            NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                sender.Header = "Settings";
                contentFrame.Navigate(typeof(SettingPage));
            }
            else
            {
                var selectedItem = (NavigationViewItem?)args.SelectedItem;
                if (selectedItem == null) return;

                string selectedItemTag = (string)selectedItem.Tag;
                sender.Header = selectedItemTag;
                string pageName = "OsuPlayer.Pages." + selectedItemTag;
                var pageType = Type.GetType(pageName);
                contentFrame.Navigate(pageType);
            }
        }
    }
}
