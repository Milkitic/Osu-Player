using Milky.OsuPlayer.ViewModels;
using Milky.WpfApi;
using OSharp.Beatmap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Lyric.Models;
using Brush = System.Drawing.Brush;
using Color = System.Drawing.Color;
using FontFamily = System.Drawing.FontFamily;
using Image = System.Drawing.Image;
using Pen = System.Drawing.Pen;
using Size = System.Windows.Size;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// LyricWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LyricWindow : WindowBase
    {
        private readonly MainWindow _mainWindow;

        public bool IsHide { get; set; }
        public MainWindowViewModel ViewModel { get; set; }

        private List<Sentence> _lyricList;
        private CancellationTokenSource _cts;
        private Task _playingTask;
        private FontFamily _fontFamily;
        private bool _pressed;

        private readonly Stopwatch _sw = new Stopwatch();
        private CancellationTokenSource _hoverCts = new CancellationTokenSource();

        public LyricWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            DataContext = _mainWindow.ViewModel;
            ViewModel = _mainWindow.ViewModel;
            InitializeComponent();

            FileInfo fi = new FileInfo(Path.Combine(Domain.ExternalPath, "font", "default.ttc"));
            if (!fi.Exists) _fontFamily = new FontFamily("等线");
            else
            {
                PrivateFontCollection pfc = new PrivateFontCollection();
                pfc.AddFontFile(fi.FullName);
                _fontFamily = pfc.Families[0];
            }

            CompositionTarget.Rendering += OnRendering;
            Left = 0;
            Top = SystemParameters.WorkArea.Height - Height - 20;
            Width = SystemParameters.PrimaryScreenWidth;
            this.MouseMove += LyricWindow_MouseMove;
            this.MouseLeave += LyricWindow_MouseLeave;
            //this.Loaded += (sender, e) =>
            //{

            //};

        }

        private void LyricWindow_MouseMove(object sender, MouseEventArgs e)
        {
            _hoverCts?.Cancel();
            _sw.Reset();
            ToolBar.Visibility = Visibility.Visible;
            ShadowBar.Visibility = Visibility.Visible;
            StrokeBar.Visibility = Visibility.Visible;
        }

        private void LyricWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            _hoverCts = new CancellationTokenSource();
            _sw.Restart();
            Task.Run(() =>
            {
                while (_sw.ElapsedMilliseconds < 1500)
                {
                    if (_hoverCts.IsCancellationRequested)
                        return;
                    Thread.Sleep(20);
                }

                Dispatcher.BeginInvoke(new Action(HideFrame));

            }, _hoverCts.Token);
        }

        private void HideFrame()
        {
            ToolBar.Visibility = Visibility.Hidden;
            ShadowBar.Visibility = Visibility.Hidden;
            StrokeBar.Visibility = Visibility.Hidden;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!_pressed) Left = 0;
        }

        public void SetNewLyric(Lyrics lyric, OsuFile osuFile)
        {
            StopWork();

            _lyricList = lyric?.LyricSentencs ?? new List<Sentence>();
            _lyricList.Insert(0,
                new Sentence(osuFile.Metadata.ArtistMeta.ToUnicodeString() + " - " + osuFile.Metadata.TitleMeta.ToUnicodeString(), 0));
        }

        public void StartWork()
        {
            StartTask();
            _playingTask = Task.Run(() =>
            {
                int oldTime = -1;
                while (!_cts.Token.IsCancellationRequested)
                {
                    Thread.Sleep(50);
                    var validLyrics = _lyricList.Where(t => t.StartTime <= ComponentPlayer.Current?.PlayTime).ToArray();
                    if (validLyrics.Length < 1)
                        continue;
                    int maxTime = validLyrics.Max(t => t.StartTime);
                    if (oldTime == maxTime)
                        continue;
                    var current = _lyricList.First(t => t.StartTime == maxTime);
                    var predictLyrics = _lyricList.Where(t => t.StartTime > maxTime);
                    Sentence? next = null;
                    if (predictLyrics.Any())
                        next = _lyricList.First(t => t.StartTime > maxTime);
                    Console.WriteLine(current.Content);

                    var size = DrawLyric(_lyricList.IndexOf(current));
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        BeginTranslate(size, maxTime, next?.StartTime ?? -1);
                    }));
                    _pressed = false;
                    oldTime = maxTime;
                }
            }, _cts.Token);
        }

        //动画定义
        private Storyboard _myStoryboard;

        private void BeginTranslate(Size size, int nowTime, int nextTime)
        {
            _myStoryboard?.Stop();
            _myStoryboard?.Remove();
            LyricBar.ClearValue(Border.MarginProperty);
            double viewWidth = 600, width = size.Width;
            if (width <= viewWidth) return;
            else Console.WriteLine($@"{size.Width}>{viewWidth}");

            //const double minInterval = 0.5;
            //if (nextTime - nowTime < minInterval) return;
            var interval = nextTime == -1 ? 4000 : (nextTime - nowTime);
            double startTime = interval / 5 > 3000 ? 3000 : interval / 5;
            double duration;
            if (nextTime == -1) duration = 3000;
            else
            {
                if (nextTime - nowTime < 10000)
                {
                    if (interval - startTime < 1000)
                    {
                        duration = interval - startTime;
                    }
                    else
                    {
                        duration = interval - startTime - 1000;
                    }
                }
                else
                {
                    duration = 10000 - startTime - 1000;
                }
            }

            Console.WriteLine($@"{0}->{viewWidth - width}, start: {startTime}, duration: {duration}");
            ThicknessAnimation defaultAnimation = new ThicknessAnimation
            {
                From = new Thickness(0),
                To = new Thickness(0),
                BeginTime = TimeSpan.FromMilliseconds(0),
                Duration = new Duration(TimeSpan.FromMilliseconds(startTime))
            };
            ThicknessAnimation translateAnimation = new ThicknessAnimation
            {
                From = new Thickness(0),
                To = new Thickness(viewWidth - width, 0, 0, 0),
                BeginTime = TimeSpan.FromMilliseconds(startTime),
                Duration = new Duration(TimeSpan.FromMilliseconds(duration))
            };

            Storyboard.SetTarget(defaultAnimation, LyricBar);
            Storyboard.SetTarget(translateAnimation, LyricBar);

            Storyboard.SetTargetProperty(defaultAnimation, new PropertyPath(Border.MarginProperty));
            Storyboard.SetTargetProperty(translateAnimation, new PropertyPath(Border.MarginProperty));

            _myStoryboard = new Storyboard();
            _myStoryboard.Children.Add(defaultAnimation);
            _myStoryboard.Children.Add(translateAnimation);

            _myStoryboard.Begin();
        }

        private Size DrawLyric(int index)
        {
            string content = _lyricList[index].Content;
            Bitmap bmp = new Bitmap(1, 1);
            SizeF size;
            using (Graphics g = Graphics.FromImage(bmp))
            using (Font f = new Font(_fontFamily, 32))
            {
                size = g.MeasureString(content, f);
            }

            int width = (int)size.Width < 1 ? 1 : (int)size.Width;
            int height = (int)size.Height < 1 ? 1 : (int)size.Height;

            bmp.Dispose();
            bmp = new Bitmap(width + 5, height + 5);
            using (StringFormat format = StringFormat.GenericTypographic)
            using (Graphics g = Graphics.FromImage(bmp))
            //using (Brush bBg = new SolidBrush(Color.FromArgb(48, 0, 176, 255)))
            //using (Pen pBg = new Pen(Color.FromArgb(192, 0, 176, 255), 3))
            using (Brush b = new TextureBrush(
                    Image.FromFile(Path.Combine(Domain.ExternalPath, "texture", "osu.png"))))
            //using (Pen p = new Pen(Color.Red))
            using (Pen p2 = new Pen(Color.FromArgb(255, 255, 255), 6))
            using (Font f = new Font(_fontFamily, 32))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                //if (_hoverd)
                //{
                //    g.DrawRectangle(pBg, 0, 0, bmp.Width - 1, bmp.Height - 1);
                //    g.FillRectangle(bBg, 0, 0, bmp.Width - 1, bmp.Height - 1);
                //}
                Rectangle rect = new Rectangle(16, 5, bmp.Width - 1, bmp.Height - 1);
                float dpi = g.DpiY;
                using (GraphicsPath gp = GetStringPath(content, dpi, rect, f, format))
                {
                    g.DrawPath(p2, gp);
                    g.FillPath(b, gp);
                }
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    BitmapImage wpfImage = new BitmapImage();
                    wpfImage.BeginInit();
                    wpfImage.StreamSource = new MemoryStream(ms.ToArray());
                    wpfImage.EndInit();

                    ImgLyric.Source = wpfImage;
                }
            }));

            return new Size(width + 5, height + 5);
        }

        private static GraphicsPath GetStringPath(string s, float dpi, RectangleF rect, Font font, StringFormat format)
        {
            GraphicsPath path = new GraphicsPath();
            // Convert font size into appropriate coordinates
            float emSize = dpi * font.SizeInPoints / 72;
            path.AddString(s, font.FontFamily, (int)font.Style, emSize, rect, format);

            return path;
        }

        public void StopWork()
        {
            CancelTask();
        }

        public void StartTask()
        {
            _cts = new CancellationTokenSource();
        }

        private void CancelTask()
        {
            _cts?.Cancel();
            if (_playingTask != null) Task.WaitAll(_playingTask);
        }

        private void ImgLyric_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void ImgLyric_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _pressed = true;
                this.DragMove();
            }

        }

        private void ImgLyric_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _pressed = false;
        }

        private void BtnHide_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        public new void Show()
        {
            IsHide = false;
            _mainWindow.ViewModel.IsLyricWindowShown = true;
            base.Show();
        }

        public new void Hide()
        {
            IsHide = true;
            _mainWindow.ViewModel.IsLyricWindowShown = false;
            base.Hide();
        }

        public void Dispose()
        {
            StopWork();
            Close();
        }

        private void BtnLock_Click(object sender, RoutedEventArgs e)
        {
            IsLocked = true;
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.BtnPrev_Click(sender, e);
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.BtnPlay_Click(sender, e);
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.BtnNext_Click(sender, e);
        }

    }
}
