using Milkitic.OsuPlayer.Utils;
using System;
using System.IO;
using System.Windows.Forms;

namespace Milkitic.OsuPlayer
{
    public partial class PlayerMain : Form
    {
        private HitsoundPlayer _hitsoundPlayer;

        public PlayerMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void LoadFile()
        {

            var openFileDialog = new OpenFileDialog
            {
                Title = @"请选择一个.osu文件",
                Filter = @"Osu Files(*.osu)|*.osu"
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;
            string path = openFileDialog.FileName;
            if (_hitsoundPlayer != null && _hitsoundPlayer.IsWorking)
            {
                _hitsoundPlayer.Stop();
            }

            _hitsoundPlayer = new HitsoundPlayer(path);
            _hitsoundPlayer.Play();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            WavePlayer.DisposeAll();
            WavePlayer.Device?.Dispose();
            WavePlayer.MasteringVoice?.Dispose();
        }

        private void BtnLoadSingle_Click(object sender, EventArgs e)
        {
            LoadFile();
        }
    }
}
