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
            AppSettings.Current.Play.GeneralOffset = (int)SliderOffset.Value;
            BoxOffset.Text = AppSettings.Current.Play.GeneralOffset.ToString();
            AppSettings.SaveCurrent();
        }

        private void BoxOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(BoxOffset.Text, out var num))
                return;
            if (num > SliderOffset.Maximum)
            {
                num = (int)SliderOffset.Maximum;
                AppSettings.Current.Play.GeneralOffset = num;
                BoxOffset.Text = AppSettings.Current.Play.GeneralOffset.ToString();
            }
            else if (num < SliderOffset.Minimum)
            {
                num = (int)SliderOffset.Minimum;
                AppSettings.Current.Play.GeneralOffset = num;
                BoxOffset.Text = AppSettings.Current.Play.GeneralOffset.ToString();
            }

            AppSettings.Current.Play.GeneralOffset = num;
            SliderOffset.Value = AppSettings.Current.Play.GeneralOffset;
            AppSettings.SaveCurrent();
        }

        private void RadioReplace_Checked(object sender, RoutedEventArgs e)
        {
            AppSettings.Current.Play.ReplacePlayList = true;
            AppSettings.SaveCurrent();
        }

        private void RadioInsert_Checked(object sender, RoutedEventArgs e)
        {
            AppSettings.Current.Play.ReplacePlayList = false;
            AppSettings.SaveCurrent();
        }

        private void ChkAutoPlay_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!ChkAutoPlay.IsChecked.HasValue)
                return;
            AppSettings.Current.Play.AutoPlay = ChkAutoPlay.IsChecked.Value;
            AppSettings.SaveCurrent();
        }

        private void ChkMemory_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!ChkMemory.IsChecked.HasValue)
                return;
            AppSettings.Current.Play.Memory = ChkMemory.IsChecked.Value;
            AppSettings.SaveCurrent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SliderOffset.Value = AppSettings.Current.Play.GeneralOffset;
            BoxOffset.Text = AppSettings.Current.Play.GeneralOffset.ToString();
            if (AppSettings.Current.Play.ReplacePlayList)
                RadioReplace.IsChecked = true;
            else
                RadioInsert.IsChecked = true;
            ChkAutoPlay.IsChecked = AppSettings.Current.Play.AutoPlay;
            ChkMemory.IsChecked = AppSettings.Current.Play.Memory;
            SliderLatency.Value = AppSettings.Current.Play.DesiredLatency;
            BoxLatency.Text = AppSettings.Current.Play.DesiredLatency.ToString();
        }

        private void BoxLatency_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(BoxLatency.Text, out var num))
                return;
            if (num > SliderLatency.Maximum)
            {
                num = (int)SliderLatency.Maximum;
                AppSettings.Current.Play.DesiredLatency = num;
                BoxLatency.Text = AppSettings.Current.Play.DesiredLatency.ToString();
            }
            else if (num < SliderLatency.Minimum)
            {
                num = (int)SliderLatency.Minimum;
                AppSettings.Current.Play.DesiredLatency = num;
                BoxLatency.Text = AppSettings.Current.Play.DesiredLatency.ToString();
            }

            AppSettings.Current.Play.DesiredLatency = num;
            SliderLatency.Value = AppSettings.Current.Play.DesiredLatency;
            AppSettings.SaveCurrent();
        }

        private void SliderLatency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AppSettings.Current.Play.DesiredLatency = (int)SliderLatency.Value;
            BoxLatency.Text = AppSettings.Current.Play.DesiredLatency.ToString();
            AppSettings.SaveCurrent();
        }
    }
}
