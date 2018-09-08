using Milkitic.OsuPlayer.Models;
using Milkitic.OsuPlayer.Utils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Milkitic.OsuPlayer
{
    public partial class PlayerMain : Form
    {
        private HitsoundPlayer _hitsoundPlayer;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _statusTask;
        private PlayStatusEnum tmpStatus = PlayStatusEnum.Stopped;

        private bool _scrollLock;

        public PlayerMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _statusTask = new Task(() =>
            {
                while (true)
                {
                    if (_cts.IsCancellationRequested)
                        return;
                    if (_hitsoundPlayer != null)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            btnControlPlayPause.Enabled = true;
                            btnControlStop.Enabled = true;
                        }));
                        if (tmpStatus != _hitsoundPlayer.PlayStatus)
                        {
                            switch (_hitsoundPlayer.PlayStatus)
                            {
                                case PlayStatusEnum.Playing:
                                    BeginInvoke(new Action(() => { btnControlPlayPause.Text = @"Pause"; }));
                                    break;
                                case PlayStatusEnum.Stopped:
                                case PlayStatusEnum.Paused:
                                    BeginInvoke(new Action(() =>
                                    {
                                        btnControlPlayPause.Text = @"Play";
                                        tkProgress.Value = Math.Min(_hitsoundPlayer.PlayTime, tkProgress.Maximum);
                                    }));
                                    break;
                            }

                            tmpStatus = _hitsoundPlayer.PlayStatus;
                        }

                        if (tmpStatus == PlayStatusEnum.Playing && !_scrollLock)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                tkProgress.Maximum = _hitsoundPlayer.Duration;
                                tkProgress.Value = Math.Min(_hitsoundPlayer.PlayTime, tkProgress.Maximum);
                            }));
                        }
                    }
                    else
                    {
                        BeginInvoke(new Action(() =>
                        {
                            btnControlPlayPause.Enabled = false;
                            btnControlStop.Enabled = false;
                        }));
                    }

                    Thread.Sleep(50);
                }
            }, _cts.Token);

            _statusTask.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ClearHitsoundPlayer();
            _cts.Cancel();
            Task.WaitAll(_statusTask);
            WavePlayer.Device?.Dispose();
            WavePlayer.MasteringVoice?.Dispose();
        }

        private void BtnLoadSingle_Click(object sender, EventArgs e)
        {
            string path = LoadFile();
            if (path == null) return;
            ClearHitsoundPlayer();
            _hitsoundPlayer = new HitsoundPlayer(path);
            _hitsoundPlayer.Play();
        }

        private void BtnControlPlayPause_Click(object sender, EventArgs e)
        {
            if (_hitsoundPlayer == null)
            {
                BtnLoadSingle_Click(sender, e);
                return;
            }

            switch (_hitsoundPlayer.PlayStatus)
            {
                case PlayStatusEnum.Playing:
                    _hitsoundPlayer.Pause();
                    break;
                case PlayStatusEnum.Stopped:
                case PlayStatusEnum.Paused:
                    _hitsoundPlayer.Play();
                    break;
            }
        }

        private void BtnControlStop_Click(object sender, EventArgs e)
        {
            _hitsoundPlayer.Stop();
        }

        private void TkProgress_MouseUp(object sender, MouseEventArgs e)
        {
            if (_hitsoundPlayer != null)
            {
                switch (_hitsoundPlayer.PlayStatus)
                {
                    case PlayStatusEnum.Playing:
                        _hitsoundPlayer.SetTime(tkProgress.Value);
                        break;
                    case PlayStatusEnum.Paused:
                    case PlayStatusEnum.Stopped:
                        _hitsoundPlayer.SetTime(tkProgress.Value, false);
                        break;
                }
            }

            _scrollLock = false;
        }

        private void TkProgress_MouseDown(object sender, MouseEventArgs e)
        {
            _scrollLock = true;
        }

        private static string LoadFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = @"请选择一个.osu文件",
                Filter = @"Osu Files(*.osu)|*.osu"
            };
            return openFileDialog.ShowDialog() != DialogResult.OK ? null : openFileDialog.FileName;
        }
        private void ClearHitsoundPlayer()
        {
            _hitsoundPlayer?.Stop();
            _hitsoundPlayer?.Dispose();
            _hitsoundPlayer = null;
        }
    }
}
