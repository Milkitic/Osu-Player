using System.Windows;
using System.Windows.Controls;
using Milki.Extensions.MixPlayer.Devices;
using Milki.OsuPlayer.Configuration;

namespace Milki.OsuPlayer.Pages.Settings;

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
        AppSettings.Default.PlaySection.GeneralOffset = (int)SliderOffset.Value;
        BoxOffset.Text = AppSettings.Default.PlaySection.GeneralOffset.ToString();
        AppSettings.SaveDefault();
    }

    private void BoxOffset_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!int.TryParse(BoxOffset.Text, out var num))
            return;
        if (num > SliderOffset.Maximum)
        {
            num = (int)SliderOffset.Maximum;
            AppSettings.Default.PlaySection.GeneralOffset = num;
            BoxOffset.Text = AppSettings.Default.PlaySection.GeneralOffset.ToString();
        }
        else if (num < SliderOffset.Minimum)
        {
            num = (int)SliderOffset.Minimum;
            AppSettings.Default.PlaySection.GeneralOffset = num;
            BoxOffset.Text = AppSettings.Default.PlaySection.GeneralOffset.ToString();
        }

        AppSettings.Default.PlaySection.GeneralOffset = num;
        SliderOffset.Value = AppSettings.Default.PlaySection.GeneralOffset;
        AppSettings.SaveDefault();
    }

    private void RadioReplace_Checked(object sender, RoutedEventArgs e)
    {
        AppSettings.Default.PlaySection.ReplacePlayList = true;
        AppSettings.SaveDefault();
    }

    private void RadioInsert_Checked(object sender, RoutedEventArgs e)
    {
        AppSettings.Default.PlaySection.ReplacePlayList = false;
        AppSettings.SaveDefault();
    }

    private void ChkAutoPlay_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (!ChkAutoPlay.IsChecked.HasValue)
            return;
        AppSettings.Default.PlaySection.AutoPlay = ChkAutoPlay.IsChecked.Value;
        AppSettings.SaveDefault();
    }

    private void ChkMemory_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (!ChkMemory.IsChecked.HasValue)
            return;
        AppSettings.Default.PlaySection.Memory = ChkMemory.IsChecked.Value;
        AppSettings.SaveDefault();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        SliderOffset.Value = AppSettings.Default.PlaySection.GeneralOffset;
        BoxOffset.Text = AppSettings.Default.PlaySection.GeneralOffset.ToString();
        if (AppSettings.Default.PlaySection.ReplacePlayList)
            RadioReplace.IsChecked = true;
        else
            RadioInsert.IsChecked = true;
        ChkAutoPlay.IsChecked = AppSettings.Default.PlaySection.AutoPlay;
        ChkMemory.IsChecked = AppSettings.Default.PlaySection.Memory;
        SliderLatency.Value = AppSettings.Default.PlaySection.DesiredLatency;
        BoxLatency.Text = AppSettings.Default.PlaySection.DesiredLatency.ToString();
        var itemsSource = DeviceCreationHelper.GetCachedAvailableDevices();
        DeviceInfoCombo.ItemsSource = itemsSource;
        if (itemsSource.Contains(AppSettings.Default.PlaySection.DeviceInfo))
        {
            DeviceInfoCombo.SelectedItem = AppSettings.Default.PlaySection.DeviceInfo;
        }
        else
        {
            DeviceInfoCombo.SelectedIndex = 0;
        }

        var selectedItem = (DeviceDescription)DeviceInfoCombo.SelectedItem;
        SliderLatency.IsEnabled = selectedItem.WavePlayerType != WavePlayerType.ASIO;
    }

    private void BoxLatency_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!int.TryParse(BoxLatency.Text, out var num))
            return;
        if (num > SliderLatency.Maximum)
        {
            num = (int)SliderLatency.Maximum;
            AppSettings.Default.PlaySection.DesiredLatency = num;
            BoxLatency.Text = AppSettings.Default.PlaySection.DesiredLatency.ToString();
        }
        else if (num < SliderLatency.Minimum)
        {
            num = (int)SliderLatency.Minimum;
            AppSettings.Default.PlaySection.DesiredLatency = num;
            BoxLatency.Text = AppSettings.Default.PlaySection.DesiredLatency.ToString();
        }

        AppSettings.Default.PlaySection.DesiredLatency = num;
        SliderLatency.Value = AppSettings.Default.PlaySection.DesiredLatency;
        AppSettings.SaveDefault();
    }

    private void SliderLatency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        AppSettings.Default.PlaySection.DesiredLatency = (int)SliderLatency.Value;
        BoxLatency.Text = AppSettings.Default.PlaySection.DesiredLatency.ToString();
        AppSettings.SaveDefault();
    }

    private void DeviceInfoCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var newVal = (DeviceDescription)e.AddedItems[0];
        SliderLatency.IsEnabled = newVal!.WavePlayerType != WavePlayerType.ASIO;
        AppSettings.Default.PlaySection.DeviceInfo = newVal;
        AppSettings.SaveDefault();
    }
}