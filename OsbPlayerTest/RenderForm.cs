using OsbPlayerTest.Layer;
using OsbPlayerTest.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Milkitic.OsbLib;
using D2D = SharpDX.Direct2D1;
using DX = SharpDX;
using DXGI = SharpDX.DXGI;
using Mathe = SharpDX.Mathematics.Interop;

namespace OsbPlayerTest
{
    internal class RenderForm : Form
    {
        private readonly ElementGroup _elementGroup;
        private D2D.Factory Factory { get; } = new D2D.Factory(D2D.FactoryType.SingleThreaded); // Factory for creating 2D elements
        private D2D.RenderTarget RenderTarget { get; set; } // Target of rendering
        public List<DxLayer> LayerList { get; set; }

        private Task[] _renderTask;

        private readonly bool _useVsync;

        public RenderForm()
        {

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = @"osb files (*.osb)|*.osb|All files (*.*)|*.*"
            };
            var result = ofd.ShowDialog();
            if (result == DialogResult.OK)
            {
                Program.Fi = new FileInfo(ofd.FileName);
            }

            var text = File.ReadAllText(Program.Fi.FullName);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine(@"Parsing..");
            ElementGroup sb = ElementGroup.Parse(text, 0);
            Console.WriteLine($@"Parse done in {sw.ElapsedMilliseconds} ms");
            sw.Stop();
            _elementGroup = sb;
            // Window settings
            ClientSize = new System.Drawing.Size(854, 480);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            // Render settings
            _useVsync = false;

            // Events
            Load += OnFormLoad;
            Shown += OnShown;
            FormClosed += OnFormClosed;
        }

        private void OnShown(object sender, EventArgs e)
        {
            TopMost = false;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            Paint += OnPaint;
            // Initial settings
            var pixelFormat = new D2D.PixelFormat(DXGI.Format.B8G8R8A8_UNorm, D2D.AlphaMode.Premultiplied);
            var winProp = new D2D.HwndRenderTargetProperties
            {
                Hwnd = Handle,
                PixelSize = new DX.Size2(ClientSize.Width, ClientSize.Height),
                PresentOptions = _useVsync ? D2D.PresentOptions.None : D2D.PresentOptions.Immediately
            };
            var renderProp = new D2D.RenderTargetProperties(D2D.RenderTargetType.Hardware, pixelFormat, 96, 96,
                D2D.RenderTargetUsage.ForceBitmapRemoting, D2D.FeatureLevel.Level_DEFAULT);
            RenderTarget = new D2D.WindowRenderTarget(Factory, renderProp, winProp)
            {
                AntialiasMode = D2D.AntialiasMode.Aliased,
                TextAntialiasMode = D2D.TextAntialiasMode.Default,
                Transform = new Mathe.RawMatrix3x2(1, 0, 0, 1, 0, 0)
            };

            LayerList = new List<DxLayer>
            {
                new BackgroundLayer(RenderTarget, _elementGroup),
                new FpsDxLayer(RenderTarget),
                //new TestLayer(RenderTarget, _obj, _osuModel),
            };

            _renderTask = new Task[LayerList.Count];

            // Avoid artifacts
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);

            Text = @"Osu!Live Player (DX)";
            LogUtil.LogInfo("Form loaded.");
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            Factory?.Dispose();
            RenderTarget?.Dispose();
            foreach (var item in LayerList)
                item?.Dispose();
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) return;
            if (RenderTarget == null || RenderTarget.IsDisposed) return;

            Render();
            Invalidate();
        }

        private void Render()
        {
            // Begin rendering
            RenderTarget.BeginDraw();
            RenderTarget.Clear(new Mathe.RawColor4(0, 0, 0, 1));

            // Draw layers
            for (var i = 0; i < LayerList.Count; i++)
            {
                var item = LayerList[i];
                _renderTask[i] = Task.Run(() => { item.Measure(); });
            }

            Task.WaitAll(_renderTask);

            foreach (var item in LayerList)
                item.Draw();

            // End drawing
            RenderTarget.TryEndDraw(out _, out _);
        }
    }
}
