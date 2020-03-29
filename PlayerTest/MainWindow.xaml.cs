using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Wave;
using OSharp.Beatmap;
using PlayerTest.Player;
using PlayerTest.Wave;

namespace PlayerTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        OsuMixPlayer _mixPlayer;
        private CancellationTokenSource _cts;
        private bool _sliderLock;

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //var path = @"E:\milkitic\others\1002455 supercell - Giniro Hikousen  (Ttm bootleg Edit)\supercell - Giniro Hikousen  (Ttm bootleg Edit) (yf_bmp) [7K Another].osu";
            //var path = @"E:\milkitic\others\Aeventyr\Grand Thaw - Aeventyr (bms2osu) [lv.12 MX].osu";
            //var path = @"D:\Games\osu!\Songs\beatmap-637113154671884689-Grand Thaw - Aventyr\Grand Thaw - Aventyr (yf_bmp) [1].osu";
            var path = @"D:\Games\osu!\Songs\1002455 supercell - Giniro Hikousen  (Ttm bootleg Edit)\supercell - Giniro Hikousen  (Ttm bootleg Edit) (yf_bmp) [7K Another].osu";
            //var path = @"D:\Games\osu!\Songs\366406 Sharlo - Melancholic\Sharlo - Melancholic (Zero__wind) [Sentimental].osu";
            //var path = @"D:\Games\osu!\Songs\O2\周杰伦 - [国服]东风破\- [] (Notefactory) [7k - hard lvl 21].osu";
            //var path = @"D:\Games\osu!\Songs\BmsToOsu\発狂難易度 (SP)\★02\Aeventyr\Grand Thaw - Aeventyr (bms2osu) [lv.12 MX].osu";
            var sw = Stopwatch.StartNew();
            var osuFile = await OsuFile.ReadFromFileAsync(path);
            Console.WriteLine(sw.ElapsedMilliseconds);
            sw.Restart();
            _mixPlayer = new OsuMixPlayer(osuFile, System.IO.Path.GetDirectoryName(path));

            await _mixPlayer.Initialize();
            Console.WriteLine(sw.ElapsedMilliseconds);
            sw.Restart();


            var duration = _mixPlayer.Duration; // 总时长
            sliderProgress.Maximum = (int)duration.TotalMilliseconds;
            lblDuration.Content = duration.ToString(@"mm\:ss");

            _cts = new CancellationTokenSource();
            StartUpdateProgress(); // 界面更新线程

            //await _mixPlayer.Play();
            Console.WriteLine(sw.ElapsedMilliseconds);
            sw.Reset();
        }

        private async void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            await _mixPlayer.Play();
        }

        private async void btnPause_Click(object sender, RoutedEventArgs e)
        {
            await _mixPlayer.Pause();
        }

        private async void btnStop_Click(object sender, RoutedEventArgs e)
        {
            await _mixPlayer.Stop();
            UpdateProgress();
        }

        private void sliderProgress_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _sliderLock = true; // 拖动开始，停止更新界面
        }

        private void sliderProgress_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (_sliderLock)
            {
                // 拖动时可以直观看到目标进度
                lblPosition.Content = TimeSpan.FromMilliseconds(sliderProgress.Value).ToString(@"mm\:ss");
            }
        }

        private async void sliderProgress_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            // 释放鼠标时，应用目标进度
            await _mixPlayer.SkipTo(TimeSpan.FromMilliseconds(sliderProgress.Value));
            UpdateProgress();
            _sliderLock = false; // 拖动结束，恢复更新界面
        }

        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateVolume();
        }

        private void StartUpdateProgress()
        {
            // 此处可用Timer完成而不是手动循环，但不建议使用UI线程上的Timer
            Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    if (_mixPlayer.PlayStatus == PlayStatus.Playing)
                    {
                        // 若为播放状态，持续更新界面
                        Dispatcher.BeginInvoke(new Action(UpdateProgress));
                        Thread.Sleep(100);
                    }
                    else
                    {
                        Thread.Sleep(50);
                    }
                }
            });
        }

        private void UpdateProgress()
        {
            var currentTime = _mixPlayer?.Position ?? TimeSpan.Zero; // 当前时间
            //Console.WriteLine(currentTime);

            if (!_sliderLock)
            {
                sliderProgress.Value = (int)currentTime.TotalMilliseconds;
                lblPosition.Content = currentTime.ToString(@"mm\:ss");
            }
        }

        private void UpdateVolume()
        {
            _mixPlayer.Volume = (float)(sliderVolume.Value / 100f);
        }
    }
}
