using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Milky.OsuPlayer.Common;

namespace Milky.OsuPlayer.UiComponents.FrontDialogComponent
{
    /// <summary>
    /// FrontDialogOverlay.xaml 的交互逻辑
    /// </summary>
    public partial class FrontDialogOverlay : UserControl
    {
        private EventHandler<DialogClosingEventArgs> _cancelAction;
        private EventHandler<DialogClosingEventArgs> _okAction;
        private Point _mouseDownPos;
        public const int DialogWidth = 800;
        public const int DialogHeight = 500;
        #region Dependency property

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set
            {
                SetValue(TitleProperty, value);
                Header.Content = value;
            }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                "Title",
                typeof(string),
                typeof(FrontDialogOverlay),
                new PropertyMetadata(""));

        public double BoxWidth
        {
            get => (double)GetValue(BoxWidthProperty);
            set
            {
                SetValue(BoxWidthProperty, value);
                BoxGrid.Width = value;
            }
        }

        public static readonly DependencyProperty BoxWidthProperty =
            DependencyProperty.Register(
                "BoxWidth",
                typeof(double),
                typeof(FrontDialogOverlay));

        public double BoxHeight
        {
            get => (double)GetValue(BoxHeightProperty);
            set
            {
                SetValue(BoxHeightProperty, value);
                BoxGrid.Height = value;
            }
        }

        public static readonly DependencyProperty BoxHeightProperty =
            DependencyProperty.Register(
                "BoxHeight",
                typeof(double),
                typeof(FrontDialogOverlay));

        public object BodyContent
        {
            get => GetValue(BodyContentProperty);
            set
            {
                SetValue(BodyContentProperty, value);

                if (value == null)
                {
                    StartCollapseAnimation();
                    //MainCanvas.Visibility = Visibility.Collapsed;
                }
                else
                {
                    StartShowAnimation();
                    Body.Content = value;
                    //MainCanvas.Visibility = Visibility.Visible;
                }
            }
        }

        public static readonly DependencyProperty BodyContentProperty =
            DependencyProperty.Register(
                "BodyContent",
                typeof(object),
                typeof(FrontDialogOverlay),
                new PropertyMetadata(null));

        #endregion

        public static FrontDialogOverlay Default { get; private set; }

        public FrontDialogOverlay()
        {
            InitializeComponent();
            if (Default == null) Default = this;
        }

        public void RaiseOk()
        {
            OK_Click(null, null);
        }

        public void RaiseCancel()
        {
            Cancel_Click(null, null);
        }

        public void ShowContent(object content, ShowContentOptions options,
            EventHandler<DialogClosingEventArgs> okAction = null,
            EventHandler<DialogClosingEventArgs> cancelAction = null)
        {
            Title = options.Title;
            BoxWidth = options.Width;
            BoxHeight = options.Height;
            _okAction = okAction;
            _cancelAction = cancelAction;

            var canvasW = MainCanvas.ActualWidth;
            var canvasH = MainCanvas.ActualHeight;

            TitleBar.Visibility = options.ShowTitleBar ? Visibility.Visible : Visibility.Collapsed;
            DialogBar.Visibility = options.ShowDialogButtons ? Visibility.Visible : Visibility.Collapsed;
            Canvas.SetLeft(BoxGrid, canvasW / 2 - options.Width / 2);
            Canvas.SetTop(BoxGrid, canvasH / 2 - options.Height / 2);
            BodyContent = content;
        }

        public void ShowContent(object content,
            string title = "",
            double width = DialogWidth,
            double height = DialogHeight,
            EventHandler<DialogClosingEventArgs> okAction = null,
            EventHandler<DialogClosingEventArgs> cancelAction = null)
        {
            ShowContent(content, new ShowContentOptions
            {
                Title = title,
                Width = width,
                Height = height
            }, okAction, cancelAction);
        }

        public FrontDialogOverlay GetOrCreateSubOverlay()
        {
            foreach (UIElement element in MainGrid.Children)
            {
                if (element is FrontDialogOverlay overlay1)
                {
                    return overlay1;
                }
            }

            var overlay = new FrontDialogOverlay();
            MainGrid.Children.Add(overlay);
            return overlay;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            BodyContent = null;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var arg = new DialogClosingEventArgs();
            _cancelAction?.Invoke(this, arg);
            if (arg.Cancel)
            {
                return;
            }

            Reset();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            var arg = new DialogClosingEventArgs();
            _okAction?.Invoke(this, arg);
            if (arg.Cancel)
            {
                return;
            }

            Reset();
        }

        private void Reset()
        {
            BodyContent = null;
        }

        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is DockPanel panel))
            {
                return;
            }

            _mouseDownPos = e.GetPosition(null);
            panel.CaptureMouse();
            panel.Cursor = Cursors.SizeAll;
        }

        private void DockPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !(sender is DockPanel panel))
            {
                return;
            }

            var preX = Canvas.GetLeft(BoxGrid);
            var preY = Canvas.GetTop(BoxGrid);
            double dx = e.GetPosition(null).X - _mouseDownPos.X + preX;
            double dy = e.GetPosition(null).Y - _mouseDownPos.Y + preY;
            Canvas.SetLeft(BoxGrid, dx);
            Canvas.SetTop(BoxGrid, dy);
            _mouseDownPos = e.GetPosition(null);
        }

        private void DockPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is DockPanel panel))
            {
                return;
            }

            panel.ReleaseMouseCapture();
            panel.Cursor = Cursors.Arrow;
        }

        private void StartCollapseAnimation()
        {
            Cover.Visibility = Visibility.Visible;
            var sb = new Storyboard();

            //var back = new BackEase { EasingMode = EasingMode.EaseIn, Amplitude = 0.3 };
            var circ = new CircleEase { EasingMode = EasingMode.EaseIn };

            var dialogTs = TimeSpan.FromMilliseconds(300);
            var dialogDuration = CommonUtils.GetDuration(dialogTs);
            var da2 = new DoubleAnimation()
            {
                From = 1,
                To = 0.7,
                Duration = dialogDuration,
                EasingFunction = circ
            };
            var da22 = da2.Clone();
            var da3 = new DoubleAnimation()
            {
                From = 1,
                To = 0,
                Duration = dialogDuration,
                EasingFunction = circ
            };

            Storyboard.SetTarget(da2, BoxGrid);
            Storyboard.SetTarget(da22, BoxGrid);
            Storyboard.SetTarget(da3, BoxGrid);
            Storyboard.SetTargetProperty(da2, new PropertyPath("RenderTransform.ScaleX"));
            Storyboard.SetTargetProperty(da22, new PropertyPath("RenderTransform.ScaleY"));
            Storyboard.SetTargetProperty(da3, new PropertyPath(OpacityProperty));

            var sine = new SineEase { EasingMode = EasingMode.EaseIn };
            var da = new DoubleAnimation
            {
                From = 1,
                To = 0,
                BeginTime = TimeSpan.FromMilliseconds(dialogTs.TotalMilliseconds / 2f),
                Duration = CommonUtils.GetDuration(TimeSpan.FromMilliseconds(150)),
                EasingFunction = sine
            };
            Storyboard.SetTarget(da, MainCanvas);
            Storyboard.SetTargetProperty(da, new PropertyPath(OpacityProperty));

            sb.Children.Add(da);
            sb.Children.Add(da2);
            sb.Children.Add(da22);
            sb.Children.Add(da3);
            sb.Completed += (sender, e) =>
            {
                MainCanvas.Visibility = Visibility.Hidden;
                Body.Content = null;
                _okAction = null;
                _cancelAction = null;

                BoxWidth = DialogWidth;
                BoxHeight = DialogHeight;
                Title = "";
                Cover.Visibility = Visibility.Hidden;
            };
            sb.Begin();
        }

        private void StartShowAnimation()
        {
            MainCanvas.Visibility = Visibility.Visible;
            var sb = new Storyboard();
            var sine = new SineEase { EasingMode = EasingMode.EaseOut };
            var canvasTs = TimeSpan.FromMilliseconds(150);
            var da = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = CommonUtils.GetDuration(canvasTs),
                EasingFunction = sine
            };
            Storyboard.SetTarget(da, MainCanvas);
            Storyboard.SetTargetProperty(da, new PropertyPath(OpacityProperty));

            var back = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
            var circ = new CircleEase { EasingMode = EasingMode.EaseOut };

            var dialogDuration = CommonUtils.GetDuration(TimeSpan.FromMilliseconds(300));
            var da2 = new DoubleAnimation
            {
                From = 0.7,
                To = 1,
                Duration = dialogDuration,
                BeginTime = TimeSpan.FromMilliseconds(canvasTs.TotalMilliseconds / 2f),
                EasingFunction = back
            };
            var da22 = da2.Clone();
            var da3 = new DoubleAnimation
            {
                From = 0,
                To = 1,
                BeginTime = TimeSpan.FromMilliseconds(canvasTs.TotalMilliseconds / 2f),
                Duration = dialogDuration,
                EasingFunction = circ
            };
            var da4 = new DoubleAnimation
            {
                From = 0,
                To = 0,
                BeginTime = TimeSpan.Zero,
                Duration = TimeSpan.Zero
            };

            Storyboard.SetTarget(da2, BoxGrid);
            Storyboard.SetTarget(da22, BoxGrid);
            Storyboard.SetTarget(da3, BoxGrid);
            Storyboard.SetTarget(da4, BoxGrid);
            Storyboard.SetTargetProperty(da2, new PropertyPath("RenderTransform.ScaleX"));
            Storyboard.SetTargetProperty(da22, new PropertyPath("RenderTransform.ScaleY"));
            Storyboard.SetTargetProperty(da3, new PropertyPath(OpacityProperty));
            Storyboard.SetTargetProperty(da4, new PropertyPath(OpacityProperty));

            sb.Children.Add(da);
            sb.Children.Add(da2);
            sb.Children.Add(da22);
            sb.Children.Add(da4);
            sb.Children.Add(da3);
            sb.Begin();
        }
        public class ShowContentOptions
        {
            public double Width { get; set; } = DialogWidth;
            public double Height { get; set; } = DialogHeight;
            public string Title { get; set; }
            public bool ShowTitleBar { get; set; } = true;
            public bool ShowDialogButtons { get; set; } = true;
        }
    }
}