using Milky.OsuPlayer.Common.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.Pages.Settings
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
            AppSettings.Default.Play.GeneralOffset = (int)SliderOffset.Value;
            BoxOffset.Text = AppSettings.Default.Play.GeneralOffset.ToString();
            AppSettings.SaveDefault();
        }

        private void BoxOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(BoxOffset.Text, out var num))
                return;
            if (num > SliderOffset.Maximum)
            {
                num = (int)SliderOffset.Maximum;
                AppSettings.Default.Play.GeneralOffset = num;
                BoxOffset.Text = AppSettings.Default.Play.GeneralOffset.ToString();
            }
            else if (num < SliderOffset.Minimum)
            {
                num = (int)SliderOffset.Minimum;
                AppSettings.Default.Play.GeneralOffset = num;
                BoxOffset.Text = AppSettings.Default.Play.GeneralOffset.ToString();
            }

            AppSettings.Default.Play.GeneralOffset = num;
            SliderOffset.Value = AppSettings.Default.Play.GeneralOffset;
            AppSettings.SaveDefault();
        }

        private void RadioReplace_Checked(object sender, RoutedEventArgs e)
        {
            AppSettings.Default.Play.ReplacePlayList = true;
            AppSettings.SaveDefault();
        }

        private void RadioInsert_Checked(object sender, RoutedEventArgs e)
        {
            AppSettings.Default.Play.ReplacePlayList = false;
            AppSettings.SaveDefault();
        }

        private void ChkAutoPlay_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!ChkAutoPlay.IsChecked.HasValue)
                return;
            AppSettings.Default.Play.AutoPlay = ChkAutoPlay.IsChecked.Value;
            AppSettings.SaveDefault();
        }

        private void ChkMemory_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!ChkMemory.IsChecked.HasValue)
                return;
            AppSettings.Default.Play.Memory = ChkMemory.IsChecked.Value;
            AppSettings.SaveDefault();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SliderOffset.Value = AppSettings.Default.Play.GeneralOffset;
            BoxOffset.Text = AppSettings.Default.Play.GeneralOffset.ToString();
            if (AppSettings.Default.Play.ReplacePlayList)
                RadioReplace.IsChecked = true;
            else
                RadioInsert.IsChecked = true;
            ChkAutoPlay.IsChecked = AppSettings.Default.Play.AutoPlay;
            ChkMemory.IsChecked = AppSettings.Default.Play.Memory;
            SliderLatency.Value = AppSettings.Default.Play.DesiredLatency;
            BoxLatency.Text = AppSettings.Default.Play.DesiredLatency.ToString();
        }

        private void BoxLatency_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(BoxLatency.Text, out var num))
                return;
            if (num > SliderLatency.Maximum)
            {
                num = (int)SliderLatency.Maximum;
                AppSettings.Default.Play.DesiredLatency = num;
                BoxLatency.Text = AppSettings.Default.Play.DesiredLatency.ToString();
            }
            else if (num < SliderLatency.Minimum)
            {
                num = (int)SliderLatency.Minimum;
                AppSettings.Default.Play.DesiredLatency = num;
                BoxLatency.Text = AppSettings.Default.Play.DesiredLatency.ToString();
            }

            AppSettings.Default.Play.DesiredLatency = num;
            SliderLatency.Value = AppSettings.Default.Play.DesiredLatency;
            AppSettings.SaveDefault();
        }

        private void SliderLatency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AppSettings.Default.Play.DesiredLatency = (int)SliderLatency.Value;
            BoxLatency.Text = AppSettings.Default.Play.DesiredLatency.ToString();
            AppSettings.SaveDefault();
        }
    }
}
