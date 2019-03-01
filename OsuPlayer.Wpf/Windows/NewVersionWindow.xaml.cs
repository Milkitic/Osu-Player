using Milky.OsuPlayer.Models.Github;
using Milky.WpfApi;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// NewVersionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NewVersionWindow : WindowBase
    {
        private readonly Release _release;
        private readonly MainWindow _mainWindow;

        public NewVersionWindow(Release release, MainWindow mainWindow)
        {
            _release = release;
            _mainWindow = mainWindow;
            InitializeComponent();
            MainGrid.DataContext = _release;
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateWindow updateWindow = new UpdateWindow(_release, _mainWindow);
            updateWindow.Show();
            Close();
        }

        private void BtnIgnore_Click(object sender, RoutedEventArgs e)
        {
            App.Config.IgnoredVer = _release.NewVerString;
            App.SaveConfig();
            Close();
        }

        private void BtnLater_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WindowBase_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }

    public static class BrowserBehavior
    {
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(BrowserBehavior),
            new FrameworkPropertyMetadata(OnHtmlChanged));

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser d)
        {
            return (string)d.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser d, string value)
        {
            d.SetValue(HtmlProperty, value);
        }

        static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WebBrowser wb)
                wb.NavigateToString((string)e.NewValue);
        }
    }
}
