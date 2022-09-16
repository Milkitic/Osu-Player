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
        AppSettings.Default.PlaySection.PlayerGeneralOffset = (int)SliderOffset.Value;
        BoxOffset.Text = AppSettings.Default.PlaySection.PlayerGeneralOffset.ToString();
        AppSettings.SaveDefault();
    }

    private void BoxOffset_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!int.TryParse(BoxOffset.Text, out var num))
            return;
        if (num > SliderOffset.Maximum)
        {
            num = (int)SliderOffset.Maximum;
            AppSettings.Default.PlaySection.PlayerGeneralOffset = num;
            BoxOffset.Text = AppSettings.Default.PlaySection.PlayerGeneralOffset.ToString();
        }
        else if (num < SliderOffset.Minimum)
        {
            num = (int)SliderOffset.Minimum;
            AppSettings.Default.PlaySection.PlayerGeneralOffset = num;
            BoxOffset.Text = AppSettings.Default.PlaySection.PlayerGeneralOffset.ToString();
        }

        AppSettings.Default.PlaySection.PlayerGeneralOffset = num;
        SliderOffset.Value = AppSettings.Default.PlaySection.PlayerGeneralOffset;
        AppSettings.SaveDefault();
    }

    private void RadioReplace_Checked(object sender, RoutedEventArgs e)
    {
        AppSettings.Default.PlaySection.IsReplacePlayList = true;
        AppSettings.SaveDefault();
    }

    private void RadioInsert_Checked(object sender, RoutedEventArgs e)
    {
        AppSettings.Default.PlaySection.IsReplacePlayList = false;
        AppSettings.SaveDefault();
    }

    private void ChkAutoPlay_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (!ChkAutoPlay.IsChecked.HasValue)
            return;
        AppSettings.Default.PlaySection.IsAutoPlayOnStartup = ChkAutoPlay.IsChecked.Value;
        AppSettings.SaveDefault();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        SliderOffset.Value = AppSettings.Default.PlaySection.PlayerGeneralOffset;
        BoxOffset.Text = AppSettings.Default.PlaySection.PlayerGeneralOffset.ToString();
        if (AppSettings.Default.PlaySection.IsReplacePlayList)
            RadioReplace.IsChecked = true;
        else
            RadioInsert.IsChecked = true;
        ChkAutoPlay.IsChecked = AppSettings.Default.PlaySection.IsAutoPlayOnStartup;
        SliderLatency.Value = AppSettings.Default.PlaySection.PlayerDesiredLatency;
        BoxLatency.Text = AppSettings.Default.PlaySection.PlayerDesiredLatency.ToString();
        await LoadDeviceList();
    }

    private async Task LoadDeviceList()
    {
        var itemsSource = (await Task.Run(DeviceCreationHelper.GetCachedAvailableDevices))
            .Where(k => k.WavePlayerType is not WavePlayerType.DirectSound).ToArray();
        DeviceInfoCombo.ItemsSource = itemsSource;
        if (itemsSource.Contains(AppSettings.Default.PlaySection.PlayerDeviceInfo))
        {
            DeviceInfoCombo.SelectedItem = AppSettings.Default.PlaySection.PlayerDeviceInfo;
        }
        else
        {
            DeviceInfoCombo.SelectedIndex = 0;
        }

        var selectedItem = (DeviceDescription)DeviceInfoCombo.SelectedItem;
        SliderLatency.IsEnabled = selectedItem?.WavePlayerType != WavePlayerType.ASIO;
    }

    private void BoxLatency_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!int.TryParse(BoxLatency.Text, out var num))
            return;
        if (num > SliderLatency.Maximum)
        {
            num = (int)SliderLatency.Maximum;
            AppSettings.Default.PlaySection.PlayerDesiredLatency = num;
            BoxLatency.Text = AppSettings.Default.PlaySection.PlayerDesiredLatency.ToString();
        }
        else if (num < SliderLatency.Minimum)
        {
            num = (int)SliderLatency.Minimum;
            AppSettings.Default.PlaySection.PlayerDesiredLatency = num;
            BoxLatency.Text = AppSettings.Default.PlaySection.PlayerDesiredLatency.ToString();
        }

        AppSettings.Default.PlaySection.PlayerDesiredLatency = num;
        SliderLatency.Value = AppSettings.Default.PlaySection.PlayerDesiredLatency;
        AppSettings.SaveDefault();
    }

    private void SliderLatency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        AppSettings.Default.PlaySection.PlayerDesiredLatency = (int)SliderLatency.Value;
        BoxLatency.Text = AppSettings.Default.PlaySection.PlayerDesiredLatency.ToString();
        AppSettings.SaveDefault();
    }

    private void DeviceInfoCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var newVal = (DeviceDescription)e.AddedItems[0];
        SliderLatency.IsEnabled = newVal!.WavePlayerType != WavePlayerType.ASIO;
        AppSettings.Default.PlaySection.PlayerDeviceInfo = newVal;
        AppSettings.SaveDefault();
    }
}