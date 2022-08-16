using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Coosu.Beatmap;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Media.Lyric.Models;
using Milki.OsuPlayer.Shared.Dependency;
using Milki.OsuPlayer.ViewModels;
using Milki.OsuPlayer.Wpf;

//using System.Drawing;
//using System.Drawing.Drawing2D;
//using System.Drawing.Text;

//using Brush = System.Drawing.Brush;
//using Color = System.Drawing.Color;
//using FontFamily = System.Drawing.FontFamily;
//using Image = System.Drawing.Image;
//using Pen = System.Drawing.Pen;

namespace Milki.OsuPlayer.Windows
{
    /// <summary>
    /// LyricWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LyricWindow : WindowEx
    {
        private readonly MainWindow _mainWindow;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public LyricWindowViewModel ViewModel { get; }
        public bool IsShown => ViewModel.IsLyricWindowShown;

        private List<Sentence> _lyricList;
        private CancellationTokenSource _cts;
        private Task _playingTask;
        //private FontFamily _fontFamily;
        private bool _pressed;

        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();

        public LyricWindow(MainWindow mainWindow) : this()
        {
            _mainWindow = mainWindow;
            InitializeComponent();

            ViewModel = (LyricWindowViewModel)DataContext;
            MainWindowViewModel.Current.LyricWindowViewModel = ViewModel;

            //var fi = new FileInfo(Path.Combine(Domain.ExtensionPath, "font", "default.ttc"));
            //if (!fi.Exists)
            //    _fontFamily = new FontFamily("等线");
            //else
            //{
            //    var pfc = new PrivateFontCollection();
            //    pfc.AddFontFile(fi.FullName);
            //    _fontFamily = pfc.Families[0];
            //}

            CompositionTarget.Rendering += OnRendering;
            Left = 0;
            Top = SystemParameters.WorkArea.Height - Height - 20;
            Width = SystemParameters.PrimaryScreenWidth;
            MouseMove += LyricWindow_MouseMove;
            MouseLeave += LyricWindow_MouseLeave;
        }

        private void LyricWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var lyricFont = AppSettings.Default.LyricSection.FontFamily ??
                            Application.Current.FindResource("SspRegular");
            ViewModel.FontFamily = lyricFont;
            ViewModel.Hue = AppSettings.Default.LyricSection.Hue;
            ViewModel.Saturation = AppSettings.Default.LyricSection.Saturation;
        }

        private void LyricWindow_MouseMove(object sender, MouseEventArgs e)
        {
            _frameTimer?.Dispose();
            ViewModel.ShowFrame = true;
        }

        private void LyricWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            _frameTimer = new Timer(state => { Execute.OnUiThread(() => ViewModel.ShowFrame = false); }, null, 1500,
                Timeout.Infinite);
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!_pressed)
                Left = 0;
        }

        public void SetNewLyric(Lyrics lyric, MetaString metaArtist, MetaString metaTitle)
        {
            StopWork();

            _lyricList = lyric?.LyricSentencs ?? new List<Sentence>();
            _lyricList.Insert(0,
                new Sentence($"{metaArtist.ToUnicodeString()} - {metaTitle.ToUnicodeString()}", 0));
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
                    var currentTime = _controller.Player?.Position ?? TimeSpan.Zero;
                    var validLyrics = _lyricList.Where(t => t.StartTime <= currentTime.TotalMilliseconds).ToArray();
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
                    Logger.Debug(current.Content);

                    var size = DrawLyric(_lyricList.IndexOf(current));
                    Execute.ToUiThread(() => { BeginTranslate(size, maxTime, next?.StartTime ?? -1); });
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
            double viewWidth = CutView.MaxWidth, width = size.Width;
            if (width <= viewWidth)
                return;
            else
                Logger.Debug($@"{size.Width}>{viewWidth}");

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

            Logger.Debug(@"{0}->{1}, start: {2}, duration: {3}",
                0, viewWidth - width, startTime, duration);
            var defaultAnimation = new ThicknessAnimation
            {
                From = new Thickness(0),
                To = new Thickness(0),
                BeginTime = TimeSpan.FromMilliseconds(0),
                Duration = /*CommonUtils.GetDuration*/(TimeSpan.FromMilliseconds(startTime))
            };
            var translateAnimation = new ThicknessAnimation
            {
                From = new Thickness(0),
                To = new Thickness(viewWidth - width - 16, 0, 0, 0),
                BeginTime = TimeSpan.FromMilliseconds(startTime),
                Duration = /*CommonUtils.GetDuration*/(TimeSpan.FromMilliseconds(duration))
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
            Size drawLyric = Size.Empty;
            bool o = false;
            Execute.OnUiThread(() =>
            {
                TbLyric.FinalSizeChanged += (size) =>
                {
                    drawLyric = TbLyric.FinalSize;
                    o = true;
                };
                TbLyric.Text = content;
            });
            var time = DateTime.Now;
            while (!o && !_cts.Token.IsCancellationRequested && DateTime.Now - time < TimeSpan.FromMilliseconds(500))
            {
                Thread.Sleep(1);
            }

            Logger.Debug(drawLyric.ToString());
            return drawLyric;
            //var bmp = new Bitmap(1, 1);
            //SizeF size;
            //using (var g = Graphics.FromImage(bmp))
            //using (var f = new Font(_fontFamily, 32))
            //{
            //    size = g.MeasureString(content, f);
            //}

            //int width = (int)size.Width < 1 ? 1 : (int)size.Width;
            //int height = (int)size.Height < 1 ? 1 : (int)size.Height;

            //bmp.Dispose();
            //bmp = new Bitmap(width + 5, height + 5);
            //using (var format = StringFormat.GenericTypographic)
            //using (var g = Graphics.FromImage(bmp))
            ////using (Brush bBg = new SolidBrush(Color.FromArgb(48, 0, 176, 255)))
            ////using (Pen pBg = new Pen(Color.FromArgb(192, 0, 176, 255), 3))
            //using (Brush b = new TextureBrush(
            //        Image.FromFile(Path.Combine(Domain.ExtensionPath, "texture", "osu.png"))))
            ////using (Pen p = new Pen(Color.Red))
            //using (var p2 = new Pen(Color.FromArgb(255, 255, 255), 6))
            //using (var f = new Font(_fontFamily, 32))
            //{
            //    g.SmoothingMode = SmoothingMode.AntiAlias;
            //    g.TextRenderingHint = TextRenderingHint.AntiAlias;

            //    var rect = new Rectangle(16, 5, bmp.Width - 1, bmp.Height - 1);
            //    float dpi = g.DpiY;
            //    using (GraphicsPath gp = GetStringPath(content, dpi, rect, f, format))
            //    {
            //        g.DrawPath(p2, gp);
            //        g.FillPath(b, gp);
            //    }
            //}

            //Execute.ToUiThread(() =>
            //{
            //    using (var ms = new MemoryStream())
            //    {
            //        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            //        var wpfImage = new BitmapImage();
            //        wpfImage.BeginInit();
            //        wpfImage.StreamSource = new MemoryStream(ms.ToArray());
            //        wpfImage.EndInit();

            //        ImgLyric.Source = wpfImage;
            //    }
            //});

            //return new Size(width + 5, height + 5);
        }

        //private static GraphicsPath GetStringPath(string s, float dpi, RectangleF rect, Font font, StringFormat format)
        //{
        //    var path = new GraphicsPath();
        //    // Convert font size into appropriate coordinates
        //    float emSize = dpi * font.SizeInPoints / 72;
        //    path.AddString(s, font.FontFamily, (int)font.Style, emSize, rect, format);

        //    return path;
        //}

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
            var meta = _controller.PlayList.CurrentInfo?.OsuFile?.Metadata;
            MetaString metaArtist = meta?.ArtistMeta ?? default;
            MetaString metaTitle = meta?.TitleMeta ?? default;
            SetNewLyric(null, metaArtist, metaTitle);
            AppSettings.Default.LyricSection.EnableLyric = true;
            AppSettings.SaveDefault();
            ViewModel.IsLyricWindowShown = true;
            _mainWindow.SetLyricSynchronously();
            base.Show();
        }

        public new void Hide()
        {
            AppSettings.Default.LyricSection.EnableLyric = false;
            AppSettings.SaveDefault();
            ViewModel.IsLyricWindowShown = false;
            CancelTask();
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
            await _controller.PlayPrevAsync();
        }

        private async void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            await _controller.Player.TogglePlay();
        }

        private async void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            await _controller.PlayNextAsync();
        }

        private async void BtnFont_Click(object sender, RoutedEventArgs e)
        {
            popFontFamily.IsOpen = true;
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            lvFontFamilies.ScrollIntoView(ViewModel.FontFamily);
        }

        private void BtnPalette_Click(object sender, RoutedEventArgs e)
        {
            popHsl.IsOpen = true;
        }

        private void sldHue_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            AppSettings.Default.LyricSection.Hue = ViewModel.Hue;
            AppSettings.SaveDefault();
        }

        private void sldSaturation_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            AppSettings.Default.LyricSection.Saturation = ViewModel.Saturation;
            AppSettings.SaveDefault();
        }

        private void sldLightness_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            AppSettings.Default.LyricSection.Lightness = ViewModel.Lightness;
            AppSettings.SaveDefault();
        }
    }
}