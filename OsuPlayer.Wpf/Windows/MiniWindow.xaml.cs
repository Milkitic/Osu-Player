using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Presentation.Interaction;
using System;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Milky.OsuPlayer.Common;
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

        private double _screenWidth;
        private double _screenHeight;

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
            _screenWidth = SystemParameters.PrimaryScreenWidth;
            _screenHeight = SystemParameters.PrimaryScreenHeight;

            var s = AppSettings.Default.General.MiniPosition;
            if (s != null && s.Length == 2)
            {
                Left = s[0];
                Top = s[1];
                if (Left > _screenWidth - ActualWidth || Left < 0)
                {
                    IsStickEnabled = true;
                }
            }
            else
            {
                Left = _screenWidth - ActualWidth - 20;
                Top = _screenHeight - ActualHeight - 100;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (!_mouseDown) return;
            var mousePos = GetMousePos();
            var old = _currentArea;
            _currentArea = Screen.GetWorkingArea(new System.Drawing.Point((int)mousePos.X, (int)mousePos.Y));
            if (old != _currentArea)
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

        private System.Windows.Point GetMousePos()
        {
            return PointToScreen(Mouse.GetPosition(this));
        }
    }
}