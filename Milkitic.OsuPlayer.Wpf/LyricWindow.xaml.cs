using Milkitic.OsuLib;
using Milkitic.OsuPlayer.Wpf.LyricExtension.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Drawing.Brush;
using Color = System.Drawing.Color;
using FontFamily = System.Drawing.FontFamily;
using Pen = System.Drawing.Pen;

namespace Milkitic.OsuPlayer.Wpf
{
    /// <summary>
    /// LyricWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LyricWindow : Window
    {
        private List<Sentence> _lyricList;
        private CancellationTokenSource _cts;
        private Task _playingTask;
        private FontFamily _fontFamily;
        private bool _hoverd;
        private bool _pressed;

        private readonly Stopwatch _sw = new Stopwatch();
        private CancellationTokenSource _hoverCts = new CancellationTokenSource();

        public LyricWindow()
        {
            InitializeComponent();

            FileInfo fi = new FileInfo(Path.Combine(Domain.ResourcePath, "font.ttc"));
            if (!fi.Exists) _fontFamily = new FontFamily("等线");
            else
            {
                PrivateFontCollection pfc = new PrivateFontCollection();
                pfc.AddFontFile(fi.FullName);
                _fontFamily = pfc.Families[0];
            }

            CompositionTarget.Rendering += OnRendering;
            Left = 0;
            Top = SystemParameters.WorkArea.Height - Height - 10;
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
            _hoverd = true;
            ToolBar.Visibility = Visibility.Visible;
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

                _hoverd = false;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ToolBar.Visibility = Visibility.Hidden;
                }));

            }, _hoverCts.Token);
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!_pressed) Left = 0;
        }

        public void SetNewLyric(Lyric lyric, OsuFile osuFile)
        {
            StopWork();

            _lyricList = lyric?.LyricSentencs ?? new List<Sentence>();
            _lyricList.Insert(0,
                new Sentence(osuFile.Metadata.GetUnicodeArtist() + " - " + osuFile.Metadata.GetUnicodeTitle(), 0));
        }

        public void StartWork()
        {
            StartTask();
            _playingTask = Task.Run(() =>
            {
                int oldTime = -1;
                bool oldhover = false;
                while (!_cts.Token.IsCancellationRequested)
                {
                    Thread.Sleep(50);
                    var sb = _lyricList.Where(t => t.StartTime < App.MusicPlayer.PlayTime).ToArray();
                    if (sb.Length < 1)
                        continue;
                    int maxTime = sb.Max(t => t.StartTime);
                    if (oldTime == maxTime && oldhover == _hoverd)
                        continue;
                    oldhover = _hoverd;
                    oldTime = maxTime;
                    var current = _lyricList.First(t => t.StartTime == maxTime);
                    Console.WriteLine(current.Content);
                    DrawLyric(_lyricList.IndexOf(current));
                    _pressed = false;
                }
            }, _cts.Token);
        }

        private void DrawLyric(int index)
        {
            string content = _lyricList[index].Content;
            Bitmap bmp = new Bitmap(1, 1);
            SizeF size;
            using (Graphics g = Graphics.FromImage(bmp))
            using (Font f = new Font(_fontFamily, 40))
            {
                size = g.MeasureString(content, f);
            }

            int width = (int)size.Width < 1 ? 1 : (int)size.Width;
            int height = (int)size.Height < 1 ? 1 : (int)size.Height;

            bmp.Dispose();
            bmp = new Bitmap(width + 5, height + 5);
            using (StringFormat format = StringFormat.GenericTypographic)
            using (Graphics g = Graphics.FromImage(bmp))
            using (Brush bBg = new SolidBrush(Color.FromArgb(48, 0, 176, 255)))
            using (Pen pBg = new Pen(Color.FromArgb(192, 0, 176, 255), 3))
            using (Brush b = new TextureBrush(
                    Image.FromFile(Path.Combine(Domain.ResourcePath, "texture", "2.png"))))
            //using (Pen p = new Pen(Color.Red))
            using (Pen p2 = new Pen(Color.FromArgb(255, 255, 255), 6))
            using (Font f = new Font(_fontFamily, 40))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                if (_hoverd)
                {
                    g.DrawRectangle(pBg, 0, 0, bmp.Width - 1, bmp.Height - 1);
                    g.FillRectangle(bBg, 0, 0, bmp.Width - 1, bmp.Height - 1);
                }
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

        public void Dispose()
        {
            StopWork();
            Close();
        }
    }
}
