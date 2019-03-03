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
            PlayerConfig.Current.Play.GeneralOffset = (int)SliderOffset.Value;
            BoxOffset.Text = PlayerConfig.Current.Play.GeneralOffset.ToString();
            PlayerConfig.SaveCurrent();
        }

        private void BoxOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(BoxOffset.Text, out var num))
                return;
            if (num > SliderOffset.Maximum)
            {
                num = (int)SliderOffset.Maximum;
                PlayerConfig.Current.Play.GeneralOffset = num;
                BoxOffset.Text = PlayerConfig.Current.Play.GeneralOffset.ToString();
            }
            else if (num < SliderOffset.Minimum)
            {
                num = (int)SliderOffset.Minimum;
                PlayerConfig.Current.Play.GeneralOffset = num;
                BoxOffset.Text = PlayerConfig.Current.Play.GeneralOffset.ToString();
            }

            PlayerConfig.Current.Play.GeneralOffset = num;
            SliderOffset.Value = PlayerConfig.Current.Play.GeneralOffset;
            PlayerConfig.SaveCurrent();
        }

        private void RadioReplace_Checked(object sender, RoutedEventArgs e)
        {
            PlayerConfig.Current.Play.ReplacePlayList = true;
            PlayerConfig.SaveCurrent();
        }

        private void RadioInsert_Checked(object sender, RoutedEventArgs e)
        {
            PlayerConfig.Current.Play.ReplacePlayList = false;
            PlayerConfig.SaveCurrent();
        }

        private void ChkAutoPlay_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!ChkAutoPlay.IsChecked.HasValue)
                return;
            PlayerConfig.Current.Play.AutoPlay = ChkAutoPlay.IsChecked.Value;
            PlayerConfig.SaveCurrent();
        }

        private void ChkMemory_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!ChkMemory.IsChecked.HasValue)
                return;
            PlayerConfig.Current.Play.Memory = ChkMemory.IsChecked.Value;
            PlayerConfig.SaveCurrent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SliderOffset.Value = PlayerConfig.Current.Play.GeneralOffset;
            BoxOffset.Text = PlayerConfig.Current.Play.GeneralOffset.ToString();
            if (PlayerConfig.Current.Play.ReplacePlayList)
                RadioReplace.IsChecked = true;
            else
                RadioInsert.IsChecked = true;
            ChkAutoPlay.IsChecked = PlayerConfig.Current.Play.AutoPlay;
            ChkMemory.IsChecked = PlayerConfig.Current.Play.Memory;
            SliderLatency.Value = PlayerConfig.Current.Play.DesiredLatency;
            BoxLatency.Text = PlayerConfig.Current.Play.DesiredLatency.ToString();
        }

        private void BoxLatency_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(BoxLatency.Text, out var num))
                return;
            if (num > SliderLatency.Maximum)
            {
                num = (int)SliderLatency.Maximum;
                PlayerConfig.Current.Play.DesiredLatency = num;
                BoxLatency.Text = PlayerConfig.Current.Play.DesiredLatency.ToString();
            }
            else if (num < SliderLatency.Minimum)
            {
                num = (int)SliderLatency.Minimum;
                PlayerConfig.Current.Play.DesiredLatency = num;
                BoxLatency.Text = PlayerConfig.Current.Play.DesiredLatency.ToString();
            }

            PlayerConfig.Current.Play.DesiredLatency = num;
            SliderLatency.Value = PlayerConfig.Current.Play.DesiredLatency;
            PlayerConfig.SaveCurrent();
        }

        private void SliderLatency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PlayerConfig.Current.Play.DesiredLatency = (int)SliderLatency.Value;
            BoxLatency.Text = PlayerConfig.Current.Play.DesiredLatency.ToString();
            PlayerConfig.SaveCurrent();
        }
    }
}
