using System.Windows;
using System.Windows.Controls.Primitives;
using Milky.OsuPlayer.Pages.Settings;
using Milky.OsuPlayer.Utils;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// ConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigWindow : EffectWindowBase
    {
        private readonly MainWindow _mainWindow;
        private readonly OptionContainer _optionContainer = new OptionContainer();
        private bool _ischanging;

        public ConfigWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
        }

        private void BtnGeneral_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content?.GetType() != typeof(GeneralPage))
                MainFrame.Navigate(new GeneralPage(_mainWindow, this));
        }

        private void BtnHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content?.GetType() != typeof(HotKeyPage))
                MainFrame.Navigate(new HotKeyPage(_mainWindow));
        }

        private void BtnLyric_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content?.GetType() != typeof(LyricPage))
                MainFrame.Navigate(new LyricPage(_mainWindow));
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content?.GetType() != typeof(ExportPage))
                MainFrame.Navigate(new ExportPage());
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content?.GetType() != typeof(AboutPage))
                MainFrame.Navigate(new AboutPage(_mainWindow, this));
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content?.GetType() != typeof(PlayPage))
                MainFrame.Navigate(new PlayPage());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BtnGeneral_Click(sender, e);
            BtnGeneral.IsChecked = true;
        }

        private void BtnNavigate_Checked(object sender, RoutedEventArgs e)
        {
            if (_ischanging) return;
            _ischanging = true;
            var btn = (ToggleButton)sender;
            _optionContainer.Switch(btn);
            _ischanging = false;
        }

        private void BtnNavigate_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_ischanging) return;
            _ischanging = true;
            ((ToggleButton)sender).IsChecked = true;
            _ischanging = false;
        }
    }
}
