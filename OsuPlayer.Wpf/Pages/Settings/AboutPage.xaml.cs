using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// AboutPage.xaml 的交互逻辑
    /// </summary>
    public partial class AboutPage : Page
    {
        private readonly string _dtFormat = "g";

        public AboutPage()
        {
            InitializeComponent();
        }

        private void LinkGithub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Milkitic/Osu-Player");
        }

        private void LinkFeedback_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Milkitic/Osu-Player/issues/new");
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentVer.Content = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            GetLastUpdate();
        }

        private void GetLastUpdate()
        {
            LastUpdate.Content = App.Config.LastUpdateCheck == null
                ? "从未"
                : App.Config.LastUpdateCheck.Value.ToString(_dtFormat);
        }

        private void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            App.Config.LastUpdateCheck = DateTime.Now;
            GetLastUpdate();
            App.SaveConfig();
        }
    }
}
