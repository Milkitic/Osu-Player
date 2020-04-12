using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Presentation.Interaction;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Milky.OsuPlayer.Common;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// MiniWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MiniWindow : WindowEx
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private double _screenWidth;
        private double _screenHeight;
        private bool _stick;

        private bool _isShowing;

        public bool Stick
        {
            get => _stick;
            private set
            {
                if (Equals(_stick, value)) return;
                _stick = value;
                StickChanged?.Invoke(value);
                Logger.Debug("StickChanged Invoked: {0}", value);
            }
        }

        private static bool _mouseDown;
        private Timer _frameTimer;
        private Storyboard _sb;
        private event Action<bool> StickChanged;

        public MiniWindow()
        {
            InitializeComponent();
            StickChanged += MiniWindow_StickChanged;
        }

        private void MiniWindow_StickChanged(bool stick)
        {
            if (stick)
            {
                DelayToHide();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _screenWidth = SystemParameters.PrimaryScreenWidth;
            _screenHeight = SystemParameters.PrimaryScreenHeight;

            var s = AppSettings.Default.General.MiniPosition;
            if (s != null && s.Length == 2)
            {
                Left = s[0];
                Top = s[1];
                if (Left > _screenWidth - ActualWidth || Left < 0)
                {
                    Stick = true;
                }
            }
            else
            {
                Left = _screenWidth - ActualWidth - 20;
                Top = _screenHeight - ActualHeight - 100;
            }

            //var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            //source?.AddHook(WndProc);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppSettings.Default.General.MiniPosition = new[] { Left, Top };
            AppSettings.SaveDefault();
        }

        private void WindowEx_LocationChanged(object sender, EventArgs e)
        {
            if (!_mouseDown) return;
            _sb?.Stop();
            if (Left >= _screenWidth - ActualWidth + 8)
            {
                Left = _screenWidth - ActualWidth + 10;
                Stick = true;
                Logger.Debug("Auto Changed Location");
            }
            else if (Left <= -8)
            {
                Left = -10;
                Stick = true;
                Logger.Debug("Auto Changed Location");
            }
            else
            {
                Stick = false;
            }

            if (Top >= _screenHeight - ActualHeight + 10)
            {
                Top = _screenHeight - ActualHeight + 10;
                Logger.Debug("Auto Changed Location");
            }
            else if (Top <= -10)
            {
                Top = -10;
                Logger.Debug("Auto Changed Location");
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!Stick) return;
            if (_isShowing) return;

            Logger.Debug("Called Control_MouseMove()");
            _isShowing = true;
            StopHiding();
            ShowFromBound();
            DropShadowEffect.Opacity = 0.5;
            DropShadowEffect.Color = Colors.Black;
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!Stick) return;
            if (!_isShowing) return;
            _isShowing = false;

            Logger.Debug("Called Control_MouseLeave()");
            DelayToHide();
        }

        private void ShowFromBound()
        {
            Logger.Debug("Called ShowFromBound()");
            if (Left >= _screenWidth - ActualWidth)
            {
                CreateStoryboard(_screenWidth - ActualWidth + 10, EasingMode.EaseOut, false);

            }
            else if (Left <= -10)
            {
                CreateStoryboard(-10, EasingMode.EaseOut, false);
                //Left = -10;
            }
        }

        private void CreateStoryboard(double toValue, EasingMode easingMode, bool isHide)
        {
            _sb?.Stop();
            _sb = new Storyboard();
            var da = new DoubleAnimation(toValue, CommonUtils.GetDuration(TimeSpan.FromMilliseconds(800)))
            {
                EasingFunction = new QuarticEase { EasingMode = easingMode }
            };
            Storyboard.SetTargetProperty(da, new PropertyPath(LeftProperty));
            Storyboard.SetTarget(da, this);
            _sb.Children.Add(da);
            _sb.Completed += (sender, e) =>
            {
                _sb.Stop();
                Left = toValue;
                DropShadowEffect.Color = isHide ? Colors.DeepPink : Colors.Black;
                DropShadowEffect.Opacity = isHide ? 1 : 0.5;
            };
            _sb.Begin();
        }

        private void HideToBound()
        {
            Logger.Debug("Called HideToBound()");
            if (Left >= _screenWidth - ActualWidth)
            {
                CreateStoryboard(_screenWidth - 5 - 10, EasingMode.EaseInOut, true);
            }
            else if (Left <= -10)
            {
                CreateStoryboard(-ActualWidth + 5 + 10, EasingMode.EaseInOut, true);
            }
        }

        private void StopHiding()
        {
            Logger.Debug("Called StopHiding()");
            _frameTimer?.Dispose();
        }

        private void DelayToHide()
        {
            Logger.Debug("Called DelayToHide()");
            StopHiding();
            _frameTimer = new Timer(state => Execute.OnUiThread(HideToBound),
                null, 1500, Timeout.Infinite);
        }

        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_NCLBUTTONUP = 0x00A2;

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCLBUTTONDOWN:
                    _mouseDown = true;
                    Logger.Debug("_mouseDown is TRUE");
                    break;
                case WM_NCLBUTTONUP:
                    _mouseDown = false;
                    Logger.Debug("_mouseDown is FALSE");
                    break;
            }

            return IntPtr.Zero;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _mouseDown = true;
            Logger.Debug("_mouseDown is TRUE");
            this.DragMove();
            e.Handled = true;
        }

        private void Window_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _mouseDown = false;
            Logger.Debug("_mouseDown is FALSE");
            e.Handled = true;
        }
    }
}
