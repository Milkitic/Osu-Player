using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Utils;
using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Timer = System.Threading.Timer;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// MiniWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MiniWindow : WindowEx
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private event Action<bool> StickChanged;

        private double _equivalentScreenWidth;
        private double _equivalentScreenHeight;

        private Rectangle _currentArea;
        private bool _isStickEnabled;
        private bool _isShowing;
        private static bool _mouseDown;

        private readonly double _stickAutoWidth = 4;
        private readonly double _stickWidth = 5;
        private Timer _frameTimer;
        private Storyboard _sb;

        private double WindowMargin => MainGrid.Margin.Left;

        private double Right
        {
            get => Left + ActualWidth;
            set => Left = value - ActualWidth;
        }

        private double Bottom
        {
            get => Top + ActualHeight;
            set => Top = value - ActualHeight;
        }

        private bool IsStickEnabled
        {
            get => _isStickEnabled;
            set
            {
                if (Equals(_isStickEnabled, value)) return;
                _isStickEnabled = value;
                StickChanged?.Invoke(value);
                Logger.Debug("StickChanged Invoked: {0}", value);
            }
        }

        public MiniWindow()
        {
            InitializeComponent();
            StickChanged += MiniWindow_StickChanged;
        }

        private void MiniWindow_StickChanged(bool stick)
        {
            if (stick) DelayToHide();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var dpiScaling = GetDpiScaling();
            _equivalentScreenWidth = SystemParameters.PrimaryScreenWidth / dpiScaling;
            _equivalentScreenHeight = SystemParameters.PrimaryScreenHeight / dpiScaling;

            var s = AppSettings.Default.General.MiniPosition;
            if (s != null && s.Length == 2)
            {
                Left = s[0];
                Top = s[1];
                if (Left > _equivalentScreenWidth - ActualWidth || Left < 0)
                {
                    IsStickEnabled = true;
                }
            }
            else
            {
                Left = _equivalentScreenWidth - ActualWidth - 20;
                Top = _equivalentScreenHeight - ActualHeight - 100;
            }

            var area = AppSettings.Default.General.MiniArea;
            if (area != null && area.Length == 4)
            {
                _currentArea = new Rectangle(area[0], area[1], area[2], area[3]);
            }
            else
            {
                var workingArea =
                    Screen.GetWorkingArea(new System.Drawing.Point((int)Left, (int)Top)); // actual dpi-scaled value
                _currentArea = new Rectangle((int)(workingArea.Left / dpiScaling),
                    (int)(workingArea.Top / dpiScaling),
                    (int)(workingArea.Width / dpiScaling),
                    (int)(workingArea.Height / dpiScaling)); // converted value
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            StopHiding();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        // while determining the location these following aspects should be considered:
        // 1. Compatible with multiple screens
        // 2. Compatible with DPI scaling
        // 3. All coordinate properties' DPI of WPF controls are 96
        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (!_mouseDown) return;
            var oldArea = _currentArea; // previous screen

            var dpiScaling = GetDpiScaling();

            var equivalentMousePos = GetMousePos(); // actual dpi-scaled value
            equivalentMousePos = new System.Windows.Point(equivalentMousePos.X / dpiScaling,
                equivalentMousePos.Y / dpiScaling); // converted value

            var workingArea = Screen.GetWorkingArea(new System.Drawing.Point((int)equivalentMousePos.X,
                (int)equivalentMousePos.Y)); // actual dpi-scaled value
            _currentArea = new Rectangle((int)(workingArea.Left / dpiScaling),
                (int)(workingArea.Top / dpiScaling),
                (int)(workingArea.Width / dpiScaling),
                (int)(workingArea.Height / dpiScaling)); // converted value

            if (oldArea != _currentArea)
                Logger.Debug(_currentArea.ToString());

            _sb?.Stop();
            if (Right >= _currentArea.Right + WindowMargin - _stickAutoWidth &&
                Right <= _currentArea.Right + WindowMargin + _stickAutoWidth)
            {
                Right = _currentArea.Right + WindowMargin;
                IsStickEnabled = true;
                Logger.Debug("Auto Changed Location");
            }
            else if (Left <= _currentArea.Left - WindowMargin + _stickAutoWidth &&
                     Left >= _currentArea.Left - WindowMargin - _stickAutoWidth)
            {
                Left = _currentArea.Left - WindowMargin;
                IsStickEnabled = true;
                Logger.Debug("Auto Changed Location");
            }
            else
            {
                IsStickEnabled = false;
            }

            if (Bottom >= _currentArea.Bottom + WindowMargin - _stickAutoWidth &&
                Bottom <= _currentArea.Bottom + WindowMargin + _stickAutoWidth)
            {
                Top = _currentArea.Bottom - ActualHeight + WindowMargin;
                Logger.Debug("Auto Changed Location");
            }
            else if (Top <= _currentArea.Top - WindowMargin + _stickAutoWidth &&
                     Top >= _currentArea.Top - WindowMargin - _stickAutoWidth)
            {
                Top = _currentArea.Top - WindowMargin;
                Logger.Debug("Auto Changed Location");
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!IsStickEnabled) return;
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
            if (!IsStickEnabled) return;
            if (!_isShowing) return;
            _isShowing = false;

            Logger.Debug("Called Control_MouseLeave()");
            DelayToHide();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDown = true;
            Logger.Debug("_mouseDown is TRUE");
            DragMove();
            e.Handled = true;
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AppSettings.Default.General.MiniPosition = new[] { Left, Top };
            AppSettings.Default.General.MiniArea = new[] {
                _currentArea.X, _currentArea.Y, _currentArea.Width, _currentArea.Height,
            };
            AppSettings.SaveDefault();

            _mouseDown = false;
            Logger.Debug("_mouseDown is FALSE");
            e.Handled = true;
        }

        private void ShowFromBound()
        {
            Logger.Debug("Called ShowFromBound()");
            if (Right >= _currentArea.Right)
            {
                CreateStoryboard(_currentArea.Right - ActualWidth + WindowMargin, EasingMode.EaseOut, false);
            }
            else if (Left <= _currentArea.Left - WindowMargin)
            {
                CreateStoryboard(_currentArea.Left - WindowMargin, EasingMode.EaseOut, false);
            }
        }

        private void HideToBound()
        {
            Logger.Debug("Called HideToBound()");
            if (Right >= _currentArea.Right)
            {
                CreateStoryboard(_currentArea.Right - _stickWidth - WindowMargin, EasingMode.EaseInOut, true);
            }
            else if (Left <= _currentArea.Left - WindowMargin)
            {
                CreateStoryboard(_currentArea.Left - ActualWidth + _stickWidth + WindowMargin, EasingMode.EaseInOut,
                    true);
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

        private static System.Windows.Point GetMousePos()
        {
            return NativeUser32.GetMousePosition();
        }

        private static int GetDpi()
        {
            var propertyInfo =
                typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
            return (int?)propertyInfo?.GetValue(null, null) ?? 96;
        }

        private static double GetDpiScaling()
        {
            return GetDpi() / 96f;
        }
    }
}