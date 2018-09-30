using Milkitic.OsuLib;
using Milkitic.OsuPlayer.LyricExtension.Model;
using Milkitic.OsuPlayer;
using Milkitic.OsuPlayer.Utils;
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
using System.Windows.Forms;

namespace Milkitic.OsuPlayer.Winforms
{
    internal class LyricForm : GdipForm
    {
        private static int ScreenHeight => Screen.PrimaryScreen.Bounds.Height;
        private static int ScreenWidth => Screen.PrimaryScreen.Bounds.Width;

        private FontFamily _fontFamily;
        //private Image _backGround;

        private List<Sentence> _lyricList;
        private CancellationTokenSource _cts;
        private Task _playingTask;
        private int? _height;
        private bool _hoverd;

        private readonly Stopwatch _sw = new Stopwatch();
        private CancellationTokenSource _hoverCts = new CancellationTokenSource();

        private LyricControlForm _ctrlForm;

        public LyricForm(Bitmap bitmap) : base(bitmap)
        {
            UpdatePosition();
            Top = ScreenHeight - 150;
            FileInfo fi = new FileInfo(Path.Combine(Domain.ResourcePath, "font.ttc"));
            if (!fi.Exists) _fontFamily = new FontFamily("等线");
            else
            {
                PrivateFontCollection pfc = new PrivateFontCollection();
                pfc.AddFontFile(fi.FullName);
                _fontFamily = pfc.Families[0];
            }

            _ctrlForm = new LyricControlForm(this, ref _hoverd, ref _hoverCts, _sw);
            this.MouseMove += OnMouseMove;
            this.MouseLeave += OnMouseLeave;
        }

        private void OnMouseLeave(object sender, EventArgs e)
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
                if (!this.IsDisposed)
                    BeginInvoke(new Action(() =>
                    {
                        _ctrlForm?.Hide();
                    }));
                else
                {
                    StopWork();
                }
            }, _hoverCts.Token);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            _hoverCts?.Cancel();
            _sw.Reset();
            _hoverd = true;
            _ctrlForm.Top = Top - _ctrlForm.Height;
            _ctrlForm.Left = Left;
            _ctrlForm?.Show();
        }

        private void SetBitmap(Bitmap bitmap)
        {
            base.SetBitmap(bitmap);
            UpdatePosition();
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
            if (_lyricList == null)
                return;

            StartTask();
            _playingTask = Task.Run(() =>
            {
                int oldTime = -1;
                bool oldhover = false;
                while (true)
                {
                    Thread.Sleep(50);
                    if (_cts.Token.IsCancellationRequested)
                        return;

                    var sb = _lyricList.Where(t => t.StartTime < Core.MusicPlayer.PlayTime).ToArray();
                    if (sb.Length < 1)
                        continue;
                    int maxTime = sb.Max(t => t.StartTime);
                    if (oldTime == maxTime && oldhover == _hoverd)
                        continue;
                    oldhover = _hoverd;
                    oldTime = maxTime;
                    var current = _lyricList.First(t => t.StartTime == maxTime);
                    Console.WriteLine(current.Content);
                    if (!IsDisposed) DrawLyric(current);
                }
            }, _cts.Token);
        }

        private void DrawLyric(Sentence sentence)
        {
            Bitmap bmp = new Bitmap(1, 1);
            SizeF size;
            using (Graphics g = Graphics.FromImage(bmp))
            using (Font f = new Font(_fontFamily, 40))
            {
                size = g.MeasureString(sentence.Content, f);
            }

            int width = (int)size.Width < 1 ? 1 : (int)size.Width;
            if (_height == null) _height = (int)size.Height < 1 ? 1 : (int)size.Height;

            bmp.Dispose();
            bmp = new Bitmap(width + 5, _height.Value + 5);
            using (StringFormat format = StringFormat.GenericTypographic)
            using (Graphics g = Graphics.FromImage(bmp))
            using (Brush bBg = new SolidBrush(Color.FromArgb(48, 0, 176, 255)))
            using (Pen pBg = new Pen(Color.FromArgb(192, 0, 176, 255), 3))
            using (Brush b = new TextureBrush(Image.FromFile(Path.Combine(Domain.ResourcePath, "texture", "2.png"))))
            //using (Pen p = new Pen(Color.Red))
            using (Pen p2 = new Pen(Color.FromArgb(255, 255, 255), 6))
            using (Font f = new Font(_fontFamily, 40))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                //g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                if (_hoverd)
                {
                    g.DrawRectangle(pBg, 0, 0, bmp.Width - 1, bmp.Height - 1);
                    g.FillRectangle(bBg, 0, 0, bmp.Width - 1, bmp.Height - 1);
                }
                Rectangle rect = new Rectangle(16, 5, bmp.Width - 1, bmp.Height - 1);
                float dpi = g.DpiY;
                using (GraphicsPath gp = GetStringPath(sentence.Content, dpi, rect, f, format))
                {
                    g.DrawPath(p2, gp); //描边
                    g.FillPath(b, gp); //填充
                }
                //g.DrawString(sentence.Content, f, b, new PointF(0, 0));
                //g.DrawRectangle(p, 0, 0, bmp.Width - 1, bmp.Height - 1);
            }

            BeginInvoke(new Action(() => SetBitmap(bmp)));
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

        private void UpdatePosition()
        {
            Left = (int)(ScreenWidth / 2f - Width / 2f);
        }
    }
}
