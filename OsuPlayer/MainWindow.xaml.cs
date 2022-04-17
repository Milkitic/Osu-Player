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
using Windows.Storage;
using Coosu.Database;
using Microsoft.EntityFrameworkCore;
using OsuPlayer.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OsuPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void myButton_Click(object sender, RoutedEventArgs e)
        {
            myButton.Content = "Clicked";
        }

        private async void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            await using var appDbContext = new ApplicationDbContext();
            //await appDbContext.Database.MigrateAsync();
            var reader = new OsuDbReader(@"E:\Games\osu!\osu!.db");
            var beatmaps = reader.EnumerateDbModels();
            var syncer = new BeatmapSyncService(appDbContext);
            await syncer.SynchronizeManaged(beatmaps);
        }
    }
}
