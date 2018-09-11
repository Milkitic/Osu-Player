using Milkitic.OsuPlayer.Models;
using Milkitic.OsuPlayer.Utils;
using osu.Shared;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Milkitic.OsuPlayer.Winforms
{
    public partial class RenderForm : Form
    {
        private VolumeForm _volumeForm;
        private LyricForm _lyricForm;

        private Dictionary<(string artist, string title), BeatmapEntry[]> _currentKv =
            new Dictionary<(string artist, string title), BeatmapEntry[]>();
        //local control
        private PlayrEnum PlayrEnum { get; set; } = PlayrEnum.LoopRandom;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _statusTask;
        private bool _scrollLock;
        private bool _queryLock;
        private bool _isManualStop = true;
        private PlayStatusEnum _status = PlayStatusEnum.Stopped;
        private readonly Stopwatch _querySw = new Stopwatch();

        public RenderForm()
        {
            InitializeComponent();
            _lyricForm = new LyricForm(new Bitmap(Path.Combine(Domain.ResourcePath, "default.png")));
            _lyricForm.Show();
        }

        private async void OnLoad(object sender, EventArgs e)
        {
            tkVolume.Value = (int)(Core.Config.Volume.Main * 100);
            FillCbSortType();
            await PlayListQueryAsync();
            if (Core.BeatmapDb == null)
                btnControlNext.Enabled = false;
            RunSurfaceUpdate();
        }

        private void FillCbSortType()
        {
            cbSortType.Items.AddRange(new object[] { SortEnum.Artist, SortEnum.Title });
            cbSortType.SelectedItem = SortEnum.Artist;
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            _cts.Cancel();
            Task.WaitAll(_statusTask);
            _cts.Dispose();
            ClearHitsoundPlayer();
            WavePlayer.Device?.Dispose();
            WavePlayer.MasteringVoice?.Dispose();
        }

        private async void TbKeyword_TextChanged(object sender, EventArgs e)
        {
            await PlayListQueryAsync();
        }

        private async void CbSortType_SelectedIndexChanged(object sender, EventArgs e)
        {
            await PlayListQueryAsync();
        }
        private void PlayList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (playList.SelectedItems.Count == 0) return;
            string artist = playList.SelectedItems[0].Text;
            string title = playList.SelectedItems[0].SubItems[1].Text;
            var array = Query.GetListByTitleArtist(title, artist, Core.Beatmaps).ToArray();
            ListViewItem[] items = new ListViewItem[array.Length];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new ListViewItem
                {
                    Text = array[i].Creator,
                    Tag = array[i]
                };
                items[i].SubItems.Add(array[i].Version);
                items[i].SubItems.Add(new TimeSpan(0, 0, 0, 0, array[i].TotalTime).ToString(@"mm\:ss"));
            }
            diffList.Items.Clear();
            diffList.Items.AddRange(items);
        }

        private async void DiffList_DoubleClick(object sender, EventArgs e)
        {
            if (diffList.SelectedItems.Count == 0) return;
            _currentKv.Clear();
            BeatmapEntry map = (BeatmapEntry)diffList.SelectedItems[0].Tag;

            await FillPlayDictionaryAsync();

            _currentKv.Remove((MetaSelect.GetUnicode(map.Artist, map.ArtistUnicode),
                MetaSelect.GetUnicode(map.Title, map.TitleUnicode)));
            var path = Path.Combine(new FileInfo(Core.Config.DbPath).Directory.FullName, "Songs", map.FolderName,
                map.BeatmapFileName);
            PlayNewFile(path);
        }

        private void TkVolume_Scroll(object sender, EventArgs e) => Core.Config.Volume.Main = tkVolume.Value / 100f;

        private void MenuFile_Open_Click(object sender, EventArgs e)
        {
            PlayNewFile(LoadFile());
        }

        private void MenuPlay_Volume_Click(object sender, EventArgs e)
        {
            _volumeForm?.Dispose();
            _volumeForm = new VolumeForm();
            _volumeForm.Show();
        }

        private void BtnControlPlayPause_Click(object sender, EventArgs e)
        {
            if (Core.HitsoundPlayer == null)
            {
                PlayNewFile(LoadFile());
                return;
            }

            switch (Core.HitsoundPlayer.PlayStatus)
            {
                case PlayStatusEnum.Playing:
                    Core.HitsoundPlayer.Pause();
                    break;
                case PlayStatusEnum.Stopped:
                case PlayStatusEnum.Paused:
                    Core.HitsoundPlayer.Play();
                    break;
            }
        }

        private void BtnControlStop_Click(object sender, EventArgs e)
        {
            _isManualStop = true;
            Core.HitsoundPlayer?.Stop();
        }

        private void BtnControlNext_Click(object sender, EventArgs e)
        {
            btnControlNext.Enabled = false;
            AutoPlayNext();
        }

        private void TkProgress_MouseUp(object sender, MouseEventArgs e)
        {
            if (Core.HitsoundPlayer != null)
            {
                switch (Core.HitsoundPlayer.PlayStatus)
                {
                    case PlayStatusEnum.Playing:
                        Core.HitsoundPlayer.SetTime(tkProgress.Value);
                        break;
                    case PlayStatusEnum.Paused:
                    case PlayStatusEnum.Stopped:
                        Core.HitsoundPlayer.SetTime(tkProgress.Value, false);
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

        private void PlayNewFile(string path)
        {
            if (path == null) return;
            if (!File.Exists(path))
            {
                MessageBox.Show(string.Format(@"所选文件不存在{0}。", Core.BeatmapDb == null ?
                        "" : " ，可能是db没有及时更新。请关闭此播放器或osu后重试"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dir = new FileInfo(path).Directory.FullName;
            ClearHitsoundPlayer();
            try
            {
                Core.HitsoundPlayer = new HitsoundPlayer(path);
                _cts = new CancellationTokenSource();
                Core.HitsoundPlayer.Play();
                var lyric = Core.LyricProvider.GetLyric(Core.HitsoundPlayer.Osufile.Metadata.GetUnicodeArtist(),
                     Core.HitsoundPlayer.Osufile.Metadata.GetUnicodeTitle(), Core.MusicPlayer.Duration);
                _lyricForm.SetNewLyric(lyric, Core.HitsoundPlayer.Osufile);
                _lyricForm.StartWork();
                tkOffset.Value = Core.HitsoundPlayer.SingleOffset;
                pbBackground.Image?.Dispose();
                tssLblMeta.Text = string.Format("{0} - {1} ({2}) [{3}]",
                    Core.HitsoundPlayer.Osufile.Metadata.GetUnicodeArtist(),
                    Core.HitsoundPlayer.Osufile.Metadata.GetUnicodeTitle(), Core.HitsoundPlayer.Osufile.Metadata.Creator,
                    Core.HitsoundPlayer.Osufile.Metadata.Version);

                if (Core.HitsoundPlayer.Osufile.Events.BackgroundInfo != null)
                {
                    var bgPath = Path.Combine(dir, Core.HitsoundPlayer.Osufile.Events.BackgroundInfo.Filename);
                    pbBackground.Image = File.Exists(bgPath) ? Image.FromFile(bgPath) : null;
                }
                else
                    pbBackground.Image = null;
            }

            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    switch (ex.InnerException)
                    {
                        case OsuLib.MultiTimingSectionException e:
                            //MessageBox.Show(this, @"铺面读取时发生问题：" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            AutoPlayNext();
                            return;
                        case OsuLib.BadOsuFormatException e:
                            //MessageBox.Show(this, @"铺面读取时发生问题：" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            AutoPlayNext();
                            return;
                        case OsuLib.VersionNotSupportedException e:
                            //MessageBox.Show(this, @"铺面读取时发生问题：" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            AutoPlayNext();
                            return;
                    }
                MessageBox.Show(this, @"发生未处理的异常问题：" + ex, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AutoPlayNext();
            }
            finally
            {
                btnControlNext.Enabled = true;
            }
        }

        private async void AutoPlayNext()
        {
            if (Core.BeatmapDb == null)
                return;
            BeatmapEntry map;
            Dictionary<GameMode, BeatmapEntry[]> dic;
            if (_currentKv.Count == 0)
                if (PlayrEnum == PlayrEnum.Loop || PlayrEnum == PlayrEnum.LoopRandom)
                {
                    await FillPlayDictionaryAsync();
                }
                else
                {
                    _isManualStop = true;
                    return;
                }

            switch (PlayrEnum)
            {
                default:
                case PlayrEnum.Normal:
                case PlayrEnum.Loop:
                    dic = _currentKv.First().Value.GroupBy(k => k.GameMode).ToDictionary(k => k.Key, k => k.ToArray());
                    break;
                case PlayrEnum.Random:
                case PlayrEnum.LoopRandom:
                    var sb = _currentKv.RandomKeys().First();
                    var thing = _currentKv.First(k => k.Key.Equals(sb));
                    dic = thing.Value.GroupBy(k => k.GameMode).ToDictionary(k => k.Key, k => k.ToArray());
                    break;
            }

            if (dic.ContainsKey(GameMode.Standard))
                map = dic[GameMode.Standard].OrderBy(k => k.DiffStarRatingStandard[Mods.None]).Last();
            else if (dic.ContainsKey(GameMode.Mania))
                map = dic[GameMode.Mania].OrderBy(k => k.DiffStarRatingMania[Mods.None]).Last();
            else if (dic.ContainsKey(GameMode.CatchTheBeat))
                map = dic[GameMode.CatchTheBeat].OrderBy(k => k.DiffStarRatingCtB[Mods.None]).Last();
            else
                map = dic[GameMode.Taiko].OrderBy(k => k.DiffStarRatingTaiko[Mods.None]).Last();
            _currentKv.Remove((MetaSelect.GetUnicode(map.Artist, map.ArtistUnicode),
                MetaSelect.GetUnicode(map.Title, map.TitleUnicode)));

            var path = Path.Combine(new FileInfo(Core.Config.DbPath).Directory.FullName, "Songs", map.FolderName,
                map.BeatmapFileName);
            PlayNewFile(path);
        }

        private async Task PlayListQueryAsync()
        {
            if (Core.BeatmapDb == null)
                return;

            SortEnum sortEnum = (SortEnum)cbSortType.SelectedItem;
            _querySw.Restart();
            if (_queryLock)
                return;
            _queryLock = true;
            await Task.Run(() =>
            {
                while (_querySw.ElapsedMilliseconds < 150)
                    Thread.Sleep(1);
                _querySw.Stop();
                _queryLock = false;
                string keyword = tbKeyword.Text;
                var list = Query.GetListByKeyword(keyword, Core.Beatmaps);

                var sorted = Query.GetStringsBySortType(sortEnum, list);

                BeginInvoke(new Action(() =>
                {
                    (string, string)[] used = sorted.Select(k => (MetaSelect.GetUnicode(k.Artist, k.ArtistUnicode),
                        MetaSelect.GetUnicode(k.Title, k.TitleUnicode))).Distinct().ToArray();
                    // valuetuple may always call GC
                    if (playList.Tag != null) playList.Tag = null;
                    playList.Tag = used;
                    ListViewItem[] items = new ListViewItem[used.Length];
                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i] = new ListViewItem { Text = used[i].Item1 };
                        items[i].SubItems.Add(used[i].Item2);
                    }
                    playList.Items.Clear();
                    playList.Items.AddRange(items);
                }));
            });

        }

        private async Task FillPlayDictionaryAsync()
        {
            await Task.Run(() =>
            {
                (string, string)[] playTuple = ((string, string)[])playList.Tag;
                var artists = playTuple.Select(k => k.Item1);
                var titles = playTuple.Select(k => k.Item2);
                _currentKv = Core.Beatmaps.Where(k =>
                        artists.Contains(MetaSelect.GetUnicode(k.Artist, k.ArtistUnicode)) &&
                        titles.Contains(MetaSelect.GetUnicode(k.Title, k.TitleUnicode)))
                    .GroupBy(k => (MetaSelect.GetUnicode(k.Artist, k.ArtistUnicode),
                        MetaSelect.GetUnicode(k.Title, k.TitleUnicode)))
                    .ToDictionary(k => k.Key, k => k.ToArray());
            });
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
                if (Core.HitsoundPlayer != null)
                {
                    if (!isStart)
                        BeginInvoke(new Action(() =>
                        {
                            btnControlPlayPause.Enabled = true;
                            btnControlStop.Enabled = true;
                            tkProgress.Enabled = true;
                        }));
                    if (Core.HitsoundPlayer != null && _status != Core.HitsoundPlayer.PlayStatus)
                    {
                        var s = Core.HitsoundPlayer.PlayStatus;
                        switch (s)
                        {
                            case PlayStatusEnum.Playing:
                                _isManualStop = false;
                                BeginInvoke(new Action(() => { btnControlPlayPause.Text = @"◫"; }));
                                break;
                            case PlayStatusEnum.Stopped when !_isManualStop:
                                BeginInvoke(new Action(AutoPlayNext));
                                break;
                            case PlayStatusEnum.Stopped:
                            case PlayStatusEnum.Paused:
                                var ok = Math.Min(Core.HitsoundPlayer.PlayTime, tkProgress.Maximum);
                                BeginInvoke(new Action(() =>
                                {
                                    btnControlPlayPause.Text = @"▶";
                                    tkProgress.Value = ok < 0 ? 0 : ok;
                                    lbTime.Text = new TimeSpan(0, 0, 0, 0, Core.HitsoundPlayer.PlayTime).ToString(@"mm\:ss") + @"/" +
                                                  new TimeSpan(0, 0, 0, 0, Core.HitsoundPlayer.Duration).ToString(@"mm\:ss");
                                }));
                                break;
                        }

                        if (Core.HitsoundPlayer != null) _status = Core.HitsoundPlayer.PlayStatus;
                    }

                    if (_status == PlayStatusEnum.Playing && !_scrollLock)
                    {
                        var ok = Math.Min(Core.HitsoundPlayer.PlayTime, tkProgress.Maximum);
                        BeginInvoke(new Action(() =>
                        {
                            if (Core.HitsoundPlayer == null) return;
                            tkProgress.Maximum = Core.HitsoundPlayer.Duration;
                            tkProgress.Value = ok < 0 ? 0 : (ok > tkProgress.Maximum ? tkProgress.Maximum : ok);
                            lbTime.Text = new TimeSpan(0, 0, 0, 0, Core.HitsoundPlayer.PlayTime).ToString(@"mm\:ss") + @"/" +
                                          new TimeSpan(0, 0, 0, 0, Core.HitsoundPlayer.Duration).ToString(@"mm\:ss");
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
            Core.HitsoundPlayer?.Stop();
            Core.HitsoundPlayer?.Dispose();
            Core.HitsoundPlayer = null;
        }

        private void TkOffset_Scroll(object sender, EventArgs e)
        {
            Core.HitsoundPlayer.SingleOffset = tkOffset.Value;
            toolTip.SetToolTip(tkOffset, tkOffset.Value.ToString());
        }
    }
}
