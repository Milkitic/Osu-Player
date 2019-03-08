using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : EffectWindowBase
    {
        public PageParts Pages => new PageParts
        {
            SearchPage = new SearchPage(this),
            RecentPlayPage = new RecentPlayPage(this),
            FindPage = new FindPage(this),
            StoryboardPage = new StoryboardPage(this),
            ExportPage = new ExportPage(this),
        };
        internal MainWindowViewModel ViewModel { get; }

        public readonly LyricWindow LyricWindow;
        public ConfigWindow ConfigWindow;
        public readonly OverallKeyHook OverallKeyHook;
        public bool ForceExit = false;

        private WindowState _lastState;
        private readonly OptionContainer _optionContainer = new OptionContainer();
        private readonly OptionContainer _modeOptionContainer = new OptionContainer();

        //local player control
        private bool _scrollLock;
        private PlayerStatus _tmpStatus = PlayerStatus.Stopped;
        private double _videoOffset;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = (MainWindowViewModel)DataContext;
            PlayerViewModel.InitViewModel();
            ViewModel.Player = PlayerViewModel.Current;
            LyricWindow = new LyricWindow(this);

            LyricWindow.Show();
            OverallKeyHook = new OverallKeyHook(this);
            TryBindHotkeys();
            Unosquare.FFME.MediaElement.FFmpegDirectory = Path.Combine(Domain.PluginPath, "ffmpeg");
        }

        private bool ValidateDb()
        {
            if (App.UseDbMode)
                return true;
            MsgBox.Show(this, "你尚未初始化osu!db，因此该功能不可用。", Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return false;
        }

        private void TryBindHotkeys()
        {
            var page = new Pages.Settings.HotKeyPage(this);
            OverallKeyHook.AddKeyHook(page.PlayPause.Name, () => { BtnPlay_Click(null, null); });
            OverallKeyHook.AddKeyHook(page.Previous.Name, () =>
            {
                //TODO
            });
            OverallKeyHook.AddKeyHook(page.Next.Name, () => { BtnNext_Click(null, null); });
            OverallKeyHook.AddKeyHook(page.VolumeUp.Name, () => { PlayerConfig.Current.Volume.Main += 0.05f; });
            OverallKeyHook.AddKeyHook(page.VolumeDown.Name, () => { PlayerConfig.Current.Volume.Main -= 0.05f; });
            OverallKeyHook.AddKeyHook(page.FullMini.Name, () =>
            {
                //TODO
            });
            OverallKeyHook.AddKeyHook(page.AddToFav.Name, () =>
            {
                //TODO
            });
            OverallKeyHook.AddKeyHook(page.Lyric.Name, () =>
            {
                if (LyricWindow.IsHide)
                    LyricWindow.Show();
                else
                    LyricWindow.Hide();
            });
        }

        private const int WmExitSizeMove = 0x232;

        private IntPtr HwndMessageHook(IntPtr wnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WmExitSizeMove:
                    if (Height <= MinHeight && !ViewModel.IsMiniMode)
                    {
                        ToMiniMode();
                    }

                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        private void Button_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Button_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class PageParts
    {
        public SearchPage SearchPage { get; set; }
        public StoryboardPage StoryboardPage { get; set; }
        public RecentPlayPage RecentPlayPage { get; set; }
        public FindPage FindPage { get; set; }
        public ExportPage ExportPage { get; set; }
    }
}
