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
            if (AppSettings.Current.Lyric.EnableLyric)
                LyricWindow.Show();

            OverallKeyHook = new OverallKeyHook(this);
            TryBindHotkeys();
            Unosquare.FFME.Library.FFmpegDirectory = Path.Combine(Domain.PluginPath, "ffmpeg");
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
                        ToMiniMode();
                    }

                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }
    }
}
