using Milkitic.OsbLib;
using Milkitic.OsbLib.Extension;
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

        public RenderForm()
        {
            // Window settings
            ClientSize = new System.Drawing.Size(1280, 720);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;

            // Events
            Load += OnFormLoad;
            Shown += OnShown;
            FormClosed += OnFormClosed;
            DoubleClick += OnDoubleClick;

            Program.Manager = new Manager(this);
        }

        private void OnShown(object sender, EventArgs e)
        {
            TopMost = false;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
            Paint += OnPaint;
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            Program.Manager.Dispose();
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) return;
            if (!Program.Manager.HwndRenderBase.DisposeRequested)
                Program.Manager.HwndRenderBase.UpdateFrame();
            Invalidate();
        }

        private void OnDoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = @"osu files (*.osu)|*.osu|osb files (*.osb)|*.osb|All files (*.*)|*.*"
            };
            var result = ofd.ShowDialog();
            if (result == DialogResult.OK)
            {
                var fi = new FileInfo(ofd.FileName);
                Program.Manager.LoadStoryboard(fi);
            }
        }
    }
}
