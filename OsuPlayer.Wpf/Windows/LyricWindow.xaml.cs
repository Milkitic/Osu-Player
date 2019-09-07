using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Lyric.Models;
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
using Milky.OsuPlayer.Control;
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

        public LyricWindowViewModel ViewModel { get; }
        public bool IsShown => ViewModel.IsLyricWindowShown;

        private List<Sentence> _lyricList;
        private CancellationTokenSource _cts;
        private Task _playingTask;
        private FontFamily _fontFamily;
        private bool _pressed;

        public LyricWindow(MainWindow mainWindow) : this()
        {
            _mainWindow = mainWindow;
            InitializeComponent();

            ViewModel = (LyricWindowViewModel)DataContext;
            ViewModel.Player = PlayerViewModel.Current;
            MainWindowViewModel.Current.LyricWindowViewModel = ViewModel;

            var fi = new FileInfo(Path.Combine(Domain.ExternalPath, "font", "default.ttc"));
            if (!fi.Exists)
                _fontFamily = new FontFamily("等线");
            else
            {
                var pfc = new PrivateFontCollection();
                pfc.AddFontFile(fi.FullName);
                _fontFamily = pfc.Families[0];
            }

            CompositionTarget.Rendering += OnRendering;
            Left = 0;
            Top = SystemParameters.WorkArea.Height - Height - 20;
            Width = SystemParameters.PrimaryScreenWidth;
            MouseMove += LyricWindow_MouseMove;
            MouseLeave += LyricWindow_MouseLeave;
        }

        private void LyricWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void LyricWindow_MouseMove(object sender, MouseEventArgs e)
        {
            _frameTimer?.Dispose();
            ViewModel.ShowFrame = true;
        }

        private void LyricWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            _frameTimer = new Timer(state =>
            {
                Execute.OnUiThread(() => ViewModel.ShowFrame = false);
            }, null, 1500, Timeout.Infinite);
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!_pressed)
                Left = 0;
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
                    Execute.ToUiThread(() =>
                    {
                        BeginTranslate(size, maxTime, next?.StartTime ?? -1);
                    });
                    _pressed = false;
                    oldTime = maxTime;
                }
            }, _cts.Token);
        }

        //动画定义
        private Storyboard _myStoryboard;
        private Timer _frameTimer;

        private void BeginTranslate(Size size, int nowTime, int nextTime)
        {
            _myStoryboard?.Stop();
            _myStoryboard?.Remove();
            LyricBar.ClearValue(Border.MarginProperty);
            double viewWidth = 600, width = size.Width;
            if (width <= viewWidth)
                return;
            else
                Console.WriteLine($@"{size.Width}>{viewWidth}");

            //const double minInterval = 0.5;
            //if (nextTime - nowTime < minInterval) return;
            var interval = nextTime == -1 ? 4000 : (nextTime - nowTime);
            double startTime = interval / 5 > 3000 ? 3000 : interval / 5;
            double duration;
            if (nextTime == -1)
                duration = 3000;
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
            var defaultAnimation = new ThicknessAnimation
            {
                From = new Thickness(0),
                To = new Thickness(0),
                BeginTime = TimeSpan.FromMilliseconds(0),
                Duration = new Duration(TimeSpan.FromMilliseconds(startTime))
            };
            var translateAnimation = new ThicknessAnimation
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
            var bmp = new Bitmap(1, 1);
            SizeF size;
            using (var g = Graphics.FromImage(bmp))
            using (var f = new Font(_fontFamily, 32))
            {
                size = g.MeasureString(content, f);
            }

            int width = (int)size.Width < 1 ? 1 : (int)size.Width;
            int height = (int)size.Height < 1 ? 1 : (int)size.Height;

            bmp.Dispose();
            bmp = new Bitmap(width + 5, height + 5);
            using (var format = StringFormat.GenericTypographic)
            using (var g = Graphics.FromImage(bmp))
            //using (Brush bBg = new SolidBrush(Color.FromArgb(48, 0, 176, 255)))
            //using (Pen pBg = new Pen(Color.FromArgb(192, 0, 176, 255), 3))
            using (Brush b = new TextureBrush(
                    Image.FromFile(Path.Combine(Domain.ExternalPath, "texture", "osu.png"))))
            //using (Pen p = new Pen(Color.Red))
            using (var p2 = new Pen(Color.FromArgb(255, 255, 255), 6))
            using (var f = new Font(_fontFamily, 32))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                var rect = new Rectangle(16, 5, bmp.Width - 1, bmp.Height - 1);
                float dpi = g.DpiY;
                using (GraphicsPath gp = GetStringPath(content, dpi, rect, f, format))
                {
                    g.DrawPath(p2, gp);
                    g.FillPath(b, gp);
                }
            }

            Execute.ToUiThread(() =>
            {
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    var wpfImage = new BitmapImage();
                    wpfImage.BeginInit();
                    wpfImage.StreamSource = new MemoryStream(ms.ToArray());
                    wpfImage.EndInit();

                    ImgLyric.Source = wpfImage;

                }
            });

            return new Size(width + 5, height + 5);
        }

        private static GraphicsPath GetStringPath(string s, float dpi, RectangleF rect, Font font, StringFormat format)
        {
            var path = new GraphicsPath();
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
            if (_playingTask != null)
                Task.WaitAll(_playingTask);
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
            AppSettings.Default.Lyric.EnableLyric = true;
            AppSettings.SaveDefault();
            ViewModel.IsLyricWindowShown = true;
            base.Show();
        }

        public new void Hide()
        {
            AppSettings.Default.Lyric.EnableLyric = false;
            AppSettings.SaveDefault();
            ViewModel.IsLyricWindowShown = false;
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

        private async void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            await PlayController.Default.PlayPrev();
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            PlayController.Default.TogglePlay();
        }

        private async void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            await PlayController.Default.PlayNext();
        }
    }
}
