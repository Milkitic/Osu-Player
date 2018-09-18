using Milkitic.OsbLib;
using OsbPlayerTest.Layer;
using OsbPlayerTest.Render;
using OsbPlayerTest.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using D2D = SharpDX.Direct2D1;
using DX = SharpDX;
using DXGI = SharpDX.DXGI;
using Mathe = SharpDX.Mathematics.Interop;

namespace OsbPlayerTest
{
    internal class RenderForm : Form
    {
        private readonly ElementGroup _elementGroup;
        private readonly HwndRenderBase _hwndRenderBase;

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
            else
                Environment.Exit(0);

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

            // Events
            Load += OnFormLoad;
            Shown += OnShown;
            FormClosed += OnFormClosed;

            _hwndRenderBase = new HwndRenderBase(this);
        }

        private void OnShown(object sender, EventArgs e)
        {
            TopMost = false;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            _hwndRenderBase.AddLayers(new CustomLayer[]
            {
                new StoryboardLayer(_hwndRenderBase.RenderTarget, _elementGroup),
                new FpsLayer(_hwndRenderBase.RenderTarget),
            });
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
            Paint += OnPaint;
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            _hwndRenderBase.Dispose();
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) return;
            if (!_hwndRenderBase.DisposeRequested) _hwndRenderBase.UpdateFrame();
            Invalidate();
        }
    }
}
