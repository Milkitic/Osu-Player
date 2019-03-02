using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Milkitic.OsuPlayer.Winforms
{
    public partial class LyricControlForm : Form
    {
        private readonly Form _baseForm;
        private bool _hoverd;
        private CancellationTokenSource _cts;
        private readonly Stopwatch _sw;

        public LyricControlForm(Form baseForm, ref bool hoverd, ref CancellationTokenSource cts, Stopwatch sw)
        {
            _baseForm = baseForm;
            _hoverd = hoverd;
            _cts = cts;
            _sw = sw;
            InitializeComponent();
        }

        private void toolStrip1_MouseLeave(object sender, EventArgs e)
        {
            _cts = new CancellationTokenSource();
            _sw.Restart();
            Task.Run(() =>
            {
                while (_sw.ElapsedMilliseconds < 1500)
                {
                    if (_cts.IsCancellationRequested)
                        return;
                    Thread.Sleep(20);
                }
                _hoverd = false;
            }, _cts.Token);
        }

        private void toolStrip1_MouseMove(object sender, MouseEventArgs e)
        {
            _cts?.Cancel();
            _sw.Reset();
            _hoverd = true;
        }

        private void LyricControlForm_Load(object sender, EventArgs e)
        {

        }

        private void TsBtnHide_Click(object sender, EventArgs e)
        {
            _cts.Cancel();
            _baseForm.Close();
            this.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

        }
    }
}
