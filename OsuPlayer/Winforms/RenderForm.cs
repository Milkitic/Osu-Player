using Milkitic.OsuPlayer.Models;
using Milkitic.OsuPlayer.Utils;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Milkitic.OsuPlayer.Winforms
{
    public partial class RenderForm : Form
    {
        private HitsoundPlayer _hitsoundPlayer;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _statusTask;
        private PlayStatusEnum _tmpStatus = PlayStatusEnum.Stopped;
        private bool _scrollLock;

        private VolumeForm _volumeForm;

        public RenderForm()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            tkVolume.Value = (int)(Core.Config.Volume.Main * 100);

            RunSurfaceUpdate();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            _cts.Cancel();
            Task.WaitAll(_statusTask);
            ClearHitsoundPlayer();
            WavePlayer.Device?.Dispose();
            WavePlayer.MasteringVoice?.Dispose();
        }

        private void TkVolume_Scroll(object sender, EventArgs e) => Core.Config.Volume.Main = tkVolume.Value / 100f;

        private void MenuFile_Open_Click(object sender, EventArgs e)
        {
            PlayNewFile();
        }

        private void MenuPlay_Volume_Click(object sender, EventArgs e)
        {
            _volumeForm?.Dispose();
            _volumeForm = new VolumeForm();
            _volumeForm.Show();
        }

        private void BtnControlPlayPause_Click(object sender, EventArgs e)
        {
            if (_hitsoundPlayer == null)
            {
                PlayNewFile();
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
            _hitsoundPlayer?.Stop();
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

        private void PlayNewFile()
        {
            string path = LoadFile();
            if (path == null) return;
            if (!File.Exists(path))
            {
                MessageBox.Show(@"你选择了一个不存在的文件。");
                return;
            }

            var dir = new FileInfo(path).Directory.FullName;
            ClearHitsoundPlayer();
#if DEBUG
#endif
            try
            {
                _hitsoundPlayer = new HitsoundPlayer(path);
                _cts = new CancellationTokenSource();
                _hitsoundPlayer.Play();
                var bgPath = Path.Combine(dir, _hitsoundPlayer.Osufile.Events.BackgroundInfo.Filename);
                if (File.Exists(bgPath))
                    pbBackground.Image = Image.FromFile(bgPath);
                else
                    pbBackground.Image.Dispose();
                tssLblMeta.Text = string.Format("{0} - {1}", _hitsoundPlayer.Osufile.Metadata.GetUnicodeArtist(),
                    _hitsoundPlayer.Osufile.Metadata.GetUnicodeTitle());
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(this, @"铺面读取时发生问题：" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (FormatException ex)
            {
                MessageBox.Show(this, @"铺面读取时发生问题：" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, @"发生未处理的异常问题：" + ex.ToString(), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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

        private void RunSurfaceUpdate()
        {
            _statusTask = Task.Run(new Action(UpdateSurface), _cts.Token);
        }

        private void UpdateSurface()
        {
            while (true)
            {
                if (_cts.IsCancellationRequested) return;

                bool isStart = btnControlPlayPause.Enabled || btnControlStop.Enabled || tkProgress.Enabled;
                if (_hitsoundPlayer != null)
                {
                    if (!isStart)
                        BeginInvoke(new Action(() =>
                        {
                            btnControlPlayPause.Enabled = true;
                            btnControlStop.Enabled = true;
                            tkProgress.Enabled = true;
                        }));
                    if (_tmpStatus != _hitsoundPlayer.PlayStatus)
                    {
                        switch (_hitsoundPlayer.PlayStatus)
                        {
                            case PlayStatusEnum.Playing:
                                BeginInvoke(new Action(() => { btnControlPlayPause.Text = @"◫"; }));
                                break;
                            case PlayStatusEnum.Stopped:
                            case PlayStatusEnum.Paused:
                                var ok = Math.Min(_hitsoundPlayer.PlayTime, tkProgress.Maximum);
                                BeginInvoke(new Action(() =>
                                {
                                    btnControlPlayPause.Text = @"▶";
                                    tkProgress.Value = ok < 0 ? 0 : ok;
                                    lbTime.Text = new TimeSpan(0, 0, 0, 0, _hitsoundPlayer.PlayTime).ToString(@"mm\:ss") + @"/" +
                                                  new TimeSpan(0, 0, 0, 0, _hitsoundPlayer.Duration).ToString(@"mm\:ss");
                                }));
                                break;
                        }

                        _tmpStatus = _hitsoundPlayer.PlayStatus;
                    }

                    if (_tmpStatus == PlayStatusEnum.Playing && !_scrollLock)
                    {
                        var ok = Math.Min(_hitsoundPlayer.PlayTime, tkProgress.Maximum);
                        BeginInvoke(new Action(() =>
                        {
                            if (_hitsoundPlayer == null) return;
                            tkProgress.Maximum = _hitsoundPlayer.Duration;
                            tkProgress.Value = ok < 0 ? 0 : (ok > tkProgress.Maximum ? tkProgress.Maximum : ok);
                            lbTime.Text = new TimeSpan(0, 0, 0, 0, _hitsoundPlayer.PlayTime).ToString(@"mm\:ss") + @"/" +
                                          new TimeSpan(0, 0, 0, 0, _hitsoundPlayer.Duration).ToString(@"mm\:ss");
                        }));
                    }
                }
                else
                {
                    if (isStart)
                        BeginInvoke(new Action(() =>
                        {
                            btnControlPlayPause.Enabled = false;
                            btnControlStop.Enabled = false;
                            tkProgress.Enabled = false;
                        }));
                }

                Thread.Sleep(50);
            }
        }

        private void ClearHitsoundPlayer()
        {
            _hitsoundPlayer?.Stop();
            _hitsoundPlayer?.Dispose();
            _hitsoundPlayer = null;
        }
    }
}
