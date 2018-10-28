using DMSkin.WPF;
using Milkitic.OsuPlayer.Control;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Media;
using Milkitic.OsuPlayer.Pages;
using Milkitic.OsuPlayer.Utils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Milkitic.OsuPlayer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : DMSkinSimpleWindow
    {
        public PageParts Pages => new PageParts
        {
            SearchPage = new SearchPage(this),
            RecentPlayPage = new RecentPlayPage(this),
            FindPage = new FindPage(this),
            StoryboardPage = new StoryboardPage(this),
            ExportPage = new ExportPage(this),
        };

        public readonly LyricWindow LyricWindow;
        public ConfigWindow ConfigWindow;
        public readonly OverallKeyHook OverallKeyHook;
        public bool ForceExit = false;

        private WindowState _lastState;
        private bool _miniMode = false;
        private readonly OptionContainer _optionContainer = new OptionContainer();

        public bool FullMode => FullModeArea.Visibility == Visibility.Visible;

        //local player control
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _statusTask;
        private bool _scrollLock;
        private PlayerStatus _tmpStatus = PlayerStatus.Stopped;
        private double _videoOffset;

        public MainWindow()
        {
            InitializeComponent();
            LyricWindow = new LyricWindow();
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
            OverallKeyHook.AddKeyHook(page.VolumeUp.Name, () => { App.Config.Volume.Main += 0.05f; });
            OverallKeyHook.AddKeyHook(page.VolumeDown.Name, () => { App.Config.Volume.Main -= 0.05f; });
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
            GC.SuppressFinalize(page);
        }

        private const int WmExitSizeMove = 0x232;

        private IntPtr HwndMessageHook(IntPtr wnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WmExitSizeMove:
                    if (Height <= MinHeight && !_miniMode)
                    {
                        ToMiniMode();
                    }

                    handled = true;
                    break;
            }

            return IntPtr.Zero;
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
