using Milkitic.OsuPlayer.Utils;
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
using Milkitic.OsuPlayer.Windows;

namespace Milkitic.OsuPlayer.Pages.Settings
{
    /// <summary>
    /// HotKeyPage.xaml 的交互逻辑
    /// </summary>
    public partial class HotKeyPage : Page
    {
        private readonly MainWindow _mainWindow;
        private bool _holdingCtrl, _holdingAlt, _holdingShift;

        public HotKeyPage(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GetAllHotKeyValue(FrameGrid);
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            string name = ((TextBox)sender).Name;
            _mainWindow.OverallKeyHook.ConfigString = name;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _mainWindow.OverallKeyHook.ConfigString = null;
            TextBox_KeyUp(sender, null);
        }

        private void TextBox_Keydown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                _holdingCtrl = true;
            else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
                _holdingShift = true;
            else if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt || e.Key == Key.System)
                _holdingAlt = true;
            var textBox = (TextBox)sender;
            List<string> strs = new List<string>();

            if (_holdingCtrl) strs.Add("Ctrl + ");
            if (_holdingShift) strs.Add("Shift + ");
            if (_holdingAlt) strs.Add("Alt + ");
            var key = e.Key.ToString();
            if (e.Key != Key.System && e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl && e.Key != Key.LeftShift &&
                e.Key != Key.RightShift && e.Key != Key.LeftAlt && e.Key != Key.RightAlt)
            {
                strs.Add(e.Key.ConvertToString());
            }

            textBox.Text = string.Join("", strs);
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e == null)
            {
                _holdingCtrl = false;
                _holdingShift = false;
                _holdingAlt = false;
            }
            else if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                _holdingCtrl = false;
            else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
                _holdingShift = false;
            else if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
                _holdingAlt = false;
            var textBox = (TextBox)sender;
            GetHotKeyValue(textBox);
            App.SaveConfig();
        }

        private static void GetHotKeyValue(TextBox textBox)
        {
            var hotKey = App.Config.HotKeys.First(k => k.Name == textBox.Name);
            List<string> strs = new List<string>();

            if (hotKey.UseControlKey) strs.Add("Ctrl");
            if (hotKey.UseShiftKey) strs.Add("Shift");
            if (hotKey.UseAltKey) strs.Add("Alt");
            strs.Add(hotKey.Key.ConvertToString());

            textBox.Text = string.Join(" + ", strs);
        }

        private static void GetAllHotKeyValue(DependencyObject obj)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                Visual childVisual = (Visual)VisualTreeHelper.GetChild(obj, i);
                if (childVisual is TextBox box)
                    GetHotKeyValue(box);
            }
        }
    }
}
