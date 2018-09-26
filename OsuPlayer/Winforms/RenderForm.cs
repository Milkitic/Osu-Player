using Milkitic.OsuPlayer.Storyboard;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Milkitic.OsuPlayer.Winforms
{
    internal class RenderForm : Form
    {

        public RenderForm(Size pbBackgroundClientSize)
        {
            // Window settings
            ClientSize = pbBackgroundClientSize;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;

            // Events
            Load += OnFormLoad;
            Shown += OnShown;
            FormClosed += OnFormClosed;
            DoubleClick += OnDoubleClick;

            Core.StoryboardProvider = new StoryboardProvider(this);
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
            Core.StoryboardProvider.Dispose();
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) return;
            if (!Core.StoryboardProvider.HwndRenderBase.DisposeRequested)
                Core.StoryboardProvider.HwndRenderBase.UpdateFrame();
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
                Core.StoryboardProvider.LoadStoryboard(ofd.FileName);
            }
        }
    }
}
