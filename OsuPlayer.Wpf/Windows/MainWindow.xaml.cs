using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.WpfApi;
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
    public partial class MainWindow : WindowBase
    {
        public PageParts Pages { get; } 
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

        public MainWindow()
        {
            Pages = new PageParts
            {
                SearchPage = new SearchPage(),
                RecentPlayPage = new RecentPlayPage(),
                FindPage = new FindPage(),
                StoryboardPage = new StoryboardPage(),
                CollectionPage = new CollectionPage(),
                ExportPage = new ExportPage(),
            };

            PlayerViewModel.InitViewModel();

            InitializeComponent();
            ViewModel = (MainWindowViewModel)DataContext;
            ViewModel.Player = PlayerViewModel.Current;
            LyricWindow = new LyricWindow(this);
            if (AppSettings.Current.Lyric.EnableLyric)
                LyricWindow.Show();

            OverallKeyHook = new OverallKeyHook(this);
            Animation.Loaded += Animation_Loaded;
            TryBindHotKeys();
        }
        private void TryBindHotKeys()
        {
            var page = new Pages.Settings.HotKeyPage(this);
            OverallKeyHook.AddKeyHook(page.PlayPause.Name, () => { PlayController.Default.TogglePlay(); });
            OverallKeyHook.AddKeyHook(page.Previous.Name, () =>
            {
                //TODO
            });
            OverallKeyHook.AddKeyHook(page.Next.Name, async () => { await PlayController.Default.PlayNext(); });
            OverallKeyHook.AddKeyHook(page.VolumeUp.Name, () => { AppSettings.Current.Volume.Main += 0.05f; });
            OverallKeyHook.AddKeyHook(page.VolumeDown.Name, () => { AppSettings.Current.Volume.Main -= 0.05f; });
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
                if (LyricWindow.IsShown)
                    LyricWindow.Hide();
                else
                    LyricWindow.Show();
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
                        ViewModel.IsMiniMode = true;
                    }

                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        private void WindowBase_Deactivated(object sender, EventArgs e)
        {
            PlayController.Default.PopPlayList.IsOpen = false;
        }

        private void ButtonBase_Click(object sender, RoutedEventArgs e)
        {
            PlayController.Default.PopPlayList.IsOpen = false;
        }
    }
}
