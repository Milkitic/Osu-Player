using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Milky.OsuPlayer.Windows
{
    // MainWindow.Guide
    partial class MainWindow
    {
        private void SkipStep_Click(object sender, RoutedEventArgs e)
        {
            PlayerConfig.Current.General.FirstOpen = false;
            PlayerConfig.SaveCurrent();
            ViewModel.ShowWelcome = false;
        }

        private async void Step1_Click(object sender, RoutedEventArgs e)
        {
            var result = Util.BrowseDb(out var path);
            if (!result.HasValue || !result.Value)
            {
                WelcomeViewModel.GuideSelectedDb = false;
                return;
            }
            try
            {
                WelcomeViewModel.GuideSyncing = true;
                await InstanceManage.GetInstance<OsuDbInst>().SyncOsuDbAsync(path, false);
                WelcomeViewModel.GuideSyncing = false;
            }
            catch (Exception ex)
            {
                MsgBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                WelcomeViewModel.GuideSelectedDb = false;
            }

            WelcomeViewModel.GuideSelectedDb = true;
        }

        private void Step2_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
