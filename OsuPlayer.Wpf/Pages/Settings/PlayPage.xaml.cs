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

namespace Milkitic.OsuPlayer.Pages.Settings
{
    /// <summary>
    /// PlayPage.xaml 的交互逻辑
    /// </summary>
    public partial class PlayPage : Page
    {
        public PlayPage()
        {
            InitializeComponent();
        }

        private void SliderOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            App.Config.Play.GeneralOffset = (int)SliderOffset.Value;
            BoxOffset.Text = App.Config.Play.GeneralOffset.ToString();
            App.SaveConfig();
        }

        private void BoxOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(BoxOffset.Text, out var num))
                return;
            if (num > SliderOffset.Maximum)
            {
                num = (int)SliderOffset.Maximum;
                App.Config.Play.GeneralOffset = num;
                BoxOffset.Text = App.Config.Play.GeneralOffset.ToString();
            }
            else if (num < SliderOffset.Minimum)
            {
                num = (int)SliderOffset.Minimum;
                App.Config.Play.GeneralOffset = num;
                BoxOffset.Text = App.Config.Play.GeneralOffset.ToString();
            }
            
            App.Config.Play.GeneralOffset = num;
            SliderOffset.Value = App.Config.Play.GeneralOffset;
            App.SaveConfig();
        }

        private void RadioReplace_Checked(object sender, RoutedEventArgs e)
        {
            App.Config.Play.ReplacePlayList = true;
            App.SaveConfig();
        }

        private void RadioInsert_Checked(object sender, RoutedEventArgs e)
        {
            App.Config.Play.ReplacePlayList = false;
            App.SaveConfig();
        }

        private void ChkAutoPlay_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!ChkAutoPlay.IsChecked.HasValue)
                return;
            App.Config.Play.AutoPlay = ChkAutoPlay.IsChecked.Value;
            App.SaveConfig();
        }

        private void ChkMemory_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!ChkMemory.IsChecked.HasValue)
                return;
            App.Config.Play.Memory = ChkMemory.IsChecked.Value;
            App.SaveConfig();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SliderOffset.Value = App.Config.Play.GeneralOffset;
            BoxOffset.Text = App.Config.Play.GeneralOffset.ToString();
            if (App.Config.Play.ReplacePlayList)
                RadioReplace.IsChecked = true;
            else
                RadioInsert.IsChecked = true;
            ChkAutoPlay.IsChecked = App.Config.Play.AutoPlay;
            ChkMemory.IsChecked = App.Config.Play.Memory;
            SliderLatency.Value = App.Config.Play.DesiredLatency;
            BoxLatency.Text = App.Config.Play.DesiredLatency.ToString();
        }

        private void BoxLatency_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(BoxLatency.Text, out var num))
                return;
            if (num > SliderLatency.Maximum)
            {
                num = (int)SliderLatency.Maximum;
                App.Config.Play.DesiredLatency = num;
                BoxLatency.Text = App.Config.Play.DesiredLatency.ToString();
            }
            else if (num < SliderLatency.Minimum)
            {
                num = (int)SliderLatency.Minimum;
                App.Config.Play.DesiredLatency = num;
                BoxLatency.Text = App.Config.Play.DesiredLatency.ToString();
            }

            App.Config.Play.DesiredLatency = num;
            SliderLatency.Value = App.Config.Play.DesiredLatency;
            App.SaveConfig();
        }

        private void SliderLatency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            App.Config.Play.DesiredLatency = (int)SliderLatency.Value;
            BoxLatency.Text = App.Config.Play.DesiredLatency.ToString();
            App.SaveConfig();
        }
    }
}
