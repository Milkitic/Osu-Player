using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Utils;

namespace Milki.OsuPlayer.Pages.Settings
{
    /// <summary>
    /// InterfacePage.xaml 的交互逻辑
    /// </summary>
    public partial class InterfacePage : Page
    {
        public InterfacePage()
        {
            InitializeComponent();

            var @interface = AppSettings.Default.Interface;
            this.DataContext = @interface;
            @interface.PropertyChanged += interface_PropertyChanged;
        }

        private void interface_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            AppSettings.SaveDefault();
        }

        private void InterfacePage_OnLoaded(object sender, RoutedEventArgs e)
        {
            var l = I18NUtil.AvailableLangDic.Keys.ToList();
            Language.ItemsSource = l;
            Language.SelectedItem = I18NUtil.CurrentLocale.Key;
        }

        private void Language_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var locale = I18NUtil.AvailableLangDic[(string)e.AddedItems[0]];
            I18NUtil.SwitchToLang(locale);
            AppSettings.Default.Interface.Locale = locale;
            AppSettings.SaveDefault();
        }
    }
}
