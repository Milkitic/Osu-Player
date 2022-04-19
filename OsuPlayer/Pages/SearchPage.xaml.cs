﻿using System;
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
using Anotar.NLog;
using OsuPlayer.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OsuPlayer.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchPage : Page
    {
        public SearchPage()
        {
            this.InitializeComponent();
        }

        private async void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            await using var ctx = new ApplicationDbContext();
            var results = await ctx.SearchPlayItemsAsync(SearchTextBox.Text, BeatmapOrderOptions.Artist, 0, 5000);

            LogTo.Info("Find " + results.Results.Count + " results.");

            GridView.ItemsSource = results.Results;
        }
    }
}
