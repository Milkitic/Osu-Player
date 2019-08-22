using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Control.Notification;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using Milky.WpfApi;
using OSharp.Beatmap;
using Unosquare.FFME.Common;

namespace Milky.OsuPlayer.Control
{
    /// <summary>
    /// PlayController.xaml 的交互逻辑
    /// </summary>
    public partial class PlayController : UserControl, IDisposable
    {
        public static PlayController Default { get; private set; }

        #region Dependency Property

        public ImageSource ThumbImage
        {
            get => (ImageSource)GetValue(ThumbImageProperty);
            set => SetValue(ThumbImageProperty, value);
        }

        public static readonly DependencyProperty ThumbImageProperty =
            DependencyProperty.Register(
                "ThumbImage",
                typeof(ImageSource),
                typeof(PlayController),
                null
            );

        #endregion

        #region Denpendency Event

        public static readonly RoutedEvent OnThumbClickEvent = EventManager.RegisterRoutedEvent(
         "OnThumbClick",
         RoutingStrategy.Bubble,
         typeof(RoutedPropertyChangedEventArgs<object>),
         typeof(PlayController));

        public event RoutedEventHandler OnThumbClick
        {
            add => AddHandler(OnThumbClickEvent, value);
            remove => RemoveHandler(OnThumbClickEvent, value);
        }

        public static readonly RoutedEvent PreviewPlayEvent = EventManager.RegisterRoutedEvent(
            "PreviewPlay",
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventArgs<object>),
            typeof(PlayController));

        public event RoutedEventHandler PreviewPlay
        {
            add => AddHandler(PreviewPlayEvent, value);
            remove => RemoveHandler(PreviewPlayEvent, value);
        }

        public static readonly RoutedEvent PreviewPauseEvent = EventManager.RegisterRoutedEvent(
            "PreviewPause",
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventArgs<object>),
            typeof(PlayController));

        public event RoutedEventHandler PreviewPause
        {
            add => AddHandler(PreviewPauseEvent, value);
            remove => RemoveHandler(PreviewPauseEvent, value);
        }

        public static readonly RoutedEvent OnLikeClickEvent = EventManager.RegisterRoutedEvent(
            "OnLikeClick",
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventArgs<object>),
            typeof(PlayController));

        public event RoutedEventHandler OnLikeClick
        {
            add => AddHandler(OnLikeClickEvent, value);
            remove => RemoveHandler(OnLikeClickEvent, value);
        }

        //public static readonly RoutedEvent OnProgressDragCompleteEvent = EventManager.RegisterRoutedEvent(
        //    "OnProgressDragComplete",
        //    RoutingStrategy.Bubble,
        //    typeof(RoutedPropertyChangedEventArgs<object>),
        //    typeof(PlayController));

        //public event RoutedEventHandler OnProgressDragComplete
        //{
        //    add => AddHandler(OnProgressDragCompleteEvent, value);
        //    remove => RemoveHandler(OnProgressDragCompleteEvent, value);
        //}
        public event EventHandler<DragCompleteEventArgs> OnProgressDragComplete;

        //public static readonly RoutedEvent OnNewFileLoadedEvent = EventManager.RegisterRoutedEvent(
        //    "OnNewFileLoaded",
        //    RoutingStrategy.Bubble,
        //    typeof(RoutedPropertyChangedEventArgs<object>),
        //    typeof(PlayController));

        //public event RoutedEventHandler OnNewFileLoaded
        //{
        //    add => AddHandler(OnNewFileLoadedEvent, value);
        //    remove => RemoveHandler(OnNewFileLoadedEvent, value);
        //}
        public event EventHandler<HandledEventArgs> OnNewFileLoaded;
        #endregion


        private readonly BeatmapDbOperator _beatmapDbOperator = new BeatmapDbOperator();
        private readonly AppDbOperator _appDbOperator = new AppDbOperator();

        private readonly OptionContainer _modeOptionContainer = new OptionContainer();

        private bool _forcePaused;
        private bool _scrollLock;

        public event Action OnPlayClick;
        public event Action OnPauseClick;

        public PlayController()
        {
            InitializeComponent();
            Default = this;
        }

        public async Task PlayNewFile(Beatmap map, bool play = true)
        {
            string path = map.InOwnFolder
                ? System.IO.Path.Combine(Domain.CustomSongPath, map.FolderName, map.BeatmapFileName)
                : System.IO.Path.Combine(Domain.OsuSongPath, map.FolderName, map.BeatmapFileName);
            await PlayNewFile(path, play);
        }

        public async Task PlayNewFile(string path, bool play = true)
        {
            await InnerPlayNewFile(path, play);
        }

        public void TogglePlay()
        {
            _forcePaused = false;
            if (ComponentPlayer.Current == null)
            {
                OpenButton_Click(null, null);
                return;
            }

            switch (ComponentPlayer.Current.PlayerStatus)
            {
                case PlayerStatus.Playing:
                    {
                        var args = new RoutedEventArgs(PreviewPauseEvent, this);
                        RaiseEvent(args);
                        if (!args.Handled)
                        {
                            PauseAudio();
                        }

                        break;
                    }
                case PlayerStatus.Ready:
                case PlayerStatus.Stopped:
                case PlayerStatus.Paused:
                    {
                        var args = new RoutedEventArgs(PreviewPlayEvent, this);
                        RaiseEvent(args);
                        if (!args.Handled)
                        {
                            PlayAudio();
                        }

                        break;
                    }
            }
        }

        public async Task PlayPrev()
        {
            await PlayNextAsync(true, false);
        }

        public async Task PlayNext()
        {
            await PlayNextAsync(true, true);
        }

        public async Task OpenNew()
        {
            var path = LoadFile();
            if (path == null)
            {
                return;
            }

            await PlayNewFile(path);
            await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.None);
        }

        private async Task SetPlayMode(PlayerMode playMode)
        {
            switch (playMode)
            {
                case PlayerMode.Normal:
                    Normal.IsChecked = true;
                    break;
                case PlayerMode.Random:
                    Random.IsChecked = true;
                    break;
                case PlayerMode.Loop:
                    Loop.IsChecked = true;
                    break;
                case PlayerMode.LoopRandom:
                    LoopRandom.IsChecked = true;
                    break;
                case PlayerMode.Single:
                    Single.IsChecked = true;
                    break;
                case PlayerMode.SingleLoop:
                    SingleLoop.IsChecked = true;
                    break;
            }

            if (playMode == Services.Get<PlayerList>().PlayerMode)
            {
                return;
            }

            Services.Get<PlayerList>().PlayerMode = playMode;
            await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.IndexOnly);
            AppSettings.Current.Play.PlayListMode = playMode;
            AppSettings.SaveCurrent();
        }

        /// <summary>
        /// Call a file dialog to open custom file.
        /// </summary>
        private static string LoadFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = @"请选择一个.osu文件",
                Filter = @"Osu Files(*.osu)|*.osu"
            };
            var result = openFileDialog.ShowDialog();
            return (result.HasValue && result.Value) ? openFileDialog.FileName : null;
        }

        /// <summary>
        /// Play a new file by file path.
        /// </summary>
        private async Task InnerPlayNewFile(string path, bool play)
        {
            var sw = Stopwatch.StartNew();

            var playerInst = Services.Get<PlayersInst>();
            var dbInst = Services.Get<OsuDbInst>();
            ComponentPlayer audioPlayer = null;

            if (path == null)
                return;
            if (File.Exists(path))
            {
                try
                {
                    var osuFile = await OsuFile.ReadFromFileAsync(path); //50 ms
                    var fi = new FileInfo(path);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot locate.", fi.FullName);
                    var dir = fi.Directory.FullName;

                    /* Clear */
                    ClearHitsoundPlayer();
                    if (System.IO.Path.GetDirectoryName(AppSettings.Current.CurrentPath) !=
                        System.IO.Path.GetDirectoryName(path))
                    {
                        Services.Get<PlayersInst>()?.ClearHitsoundCache();
                    }

                    /* Set Meta */
                    var nowIdentity = new MapIdentity(fi.Directory.Name, osuFile.Metadata.Version);

                    MapInfo mapInfo = _appDbOperator.GetMapFromDb(nowIdentity);
                    Beatmap beatmap = _beatmapDbOperator.GetBeatmapByIdentifiable(nowIdentity);

                    bool isFavorite = IsMapFavorite(mapInfo); //50 ms

                    var info = Services.Get<PlayerList>().CurrentInfo;
                    if (info != null)
                    {
                        info.Artist = osuFile.Metadata.Artist;
                        info.ArtistUnicode = osuFile.Metadata.ArtistUnicode;
                        info.Title = osuFile.Metadata.Title;
                        info.TitleUnicode = osuFile.Metadata.TitleUnicode;
                    }

                    LblNow.Visibility = Visibility.Hidden;
                    LblTotal.Visibility = Visibility.Hidden;
                    PlayProgress.Maximum = 1;
                    PlayProgress.Value = 0;

                    /* Set Thumb */
                    if (osuFile.Events.BackgroundInfo != null)
                    {
                        var bgPath = System.IO.Path.Combine(dir, osuFile.Events.BackgroundInfo.Filename);
                        Thumb.Source = File.Exists(bgPath) ? new BitmapImage(new Uri(bgPath)) : null;
                    }

                    /* Set new hitsound player*/
                    playerInst.SetAudioPlayer(path, osuFile);
                    audioPlayer = playerInst.AudioPlayer;
                    SignUpPlayerEvent(audioPlayer);
                    await audioPlayer.InitializeAsync(); //700 ms
                    audioPlayer.HitsoundOffset = mapInfo.Offset;
                    Offset.Value = audioPlayer.HitsoundOffset;

                    var currentInfo = new CurrentInfo(
                        osuFile.Metadata.Artist,
                        osuFile.Metadata.ArtistUnicode,
                        osuFile.Metadata.Title,
                        osuFile.Metadata.TitleUnicode,
                        osuFile.Metadata.Creator,
                        osuFile.Metadata.Source,
                        osuFile.Metadata.TagList,
                        osuFile.Metadata.BeatmapId,
                        osuFile.Metadata.BeatmapSetId,
                        beatmap?.DiffSrNoneStandard ?? 0,
                        osuFile.Difficulty.HpDrainRate,
                        osuFile.Difficulty.CircleSize,
                        osuFile.Difficulty.ApproachRate,
                        osuFile.Difficulty.OverallDifficulty,
                        audioPlayer.Duration,
                        nowIdentity,
                        mapInfo,
                        beatmap,
                        isFavorite, path); // 20 ms
                    Services.Get<PlayerList>().CurrentInfo = currentInfo;
                    PlayerViewModel.Current.CurrentInfo = currentInfo;

                    /* Set Progress */
                    PlayProgress.Maximum = audioPlayer.Duration;
                    PlayProgress.Value = 0;

                    PlayerViewModel.Current.Duration = Services.Get<PlayersInst>().AudioPlayer.Duration;

                    /* Start Play */
                    //var args = new RoutedEventArgs(OnNewFileLoadedEvent, this);
                    //RaiseEvent(args);
                    var args = new HandledEventArgs();
                    OnNewFileLoaded?.BeginInvoke(this, args, obj =>
                    {
                        if (!args.Handled)
                        {
                            if (play)
                            {
                                audioPlayer.Play();
                            }
                        }
                    }, null);

                    LblNow.Visibility = Visibility.Visible;
                    LblTotal.Visibility = Visibility.Visible;
                    AppSettings.Current.CurrentPath = path;
                    AppSettings.SaveCurrent();

                    _appDbOperator.UpdateMap(nowIdentity);
                }
                catch (Exception ex)
                {
                    App.NotificationList.Add(new NotificationOption
                    {
                        Content = @"发生未处理的错误：" + (ex.InnerException ?? ex)
                    });

                    if (audioPlayer == null) return;
                    if (audioPlayer.PlayerStatus != PlayerStatus.Playing)
                    {
                        await PlayNextAsync(false, true);
                    }
                }
            }
            else
            {
                App.NotificationList.Add(new NotificationOption
                {
                    Content = @"所选文件不存在，可能是db没有及时更新。请尝试手动同步osu db后重试。"
                });
            }

            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        private void PlayAudio()
        {
            if (_forcePaused)
            {
                return;
            }

            Services.Get<PlayersInst>().AudioPlayer.Play();
            OnPlayClick?.Invoke();
        }

        private void PauseAudio()
        {
            Services.Get<PlayersInst>().AudioPlayer.Pause();
            OnPauseClick?.Invoke();
        }

        private void SignUpPlayerEvent(ComponentPlayer audioPlayer)
        {
            audioPlayer.PlayerLoaded += (sender, e) =>
            {
                var player = (ComponentPlayer)sender;
                Console.WriteLine(player.OsuFile.ToString() + @" PlayerLoaded.");
            };
            audioPlayer.PlayerFinished += async (sender, e) => { await PlayNextAsync(false, true); };
            audioPlayer.PlayerPaused += (sender, e) =>
            {
                PlayerViewModel.Current.IsPlaying = false;
                PlayerViewModel.Current.Position = e.Position;
            };
            audioPlayer.PositionSet += (sender, e) => { };
            audioPlayer.PlayerStarted += (sender, e) =>
            {
                PlayerViewModel.Current.IsPlaying = true;
                PlayerViewModel.Current.Position = e.Position;
            };
            audioPlayer.PlayerStopped += (sender, e) => { };
            audioPlayer.PositionChanged += (sender, e) =>
            {
                if (!_scrollLock)
                {
                    PlayerViewModel.Current.Position = e.Position;
                    PlayProgress.Value = e.Position;
                }
            };
        }

        private bool IsMapFavorite(MapInfo info)
        {
            var album = _appDbOperator.GetCollectionsByMap(info);
            bool isFavorite = album != null && album.Any(k => k.Locked);

            return isFavorite;
        }

        /// <summary>
        /// Play next song in list if list exist.
        /// </summary>
        /// <param name="isManual">Whether it is called by user (Click next button manually)
        /// or called by application (A song finished).</param>
        /// <param name="isNext"></param>
        private async Task PlayNextAsync(bool isManual, bool isNext)
        {
            if (Services.Get<PlayersInst>().AudioPlayer == null)
            {
                return;
            }

            (PlayerList.ChangeType result, Beatmap map) =
                await Services.Get<PlayerList>().PlayToAsync(isNext, isManual);
            switch (result)
            {
                case PlayerList.ChangeType.Stop:
                    PlayerViewModel.Current.IsPlaying = false;
                    PlayerViewModel.Current.Position = 0;
                    _forcePaused = true;
                    break;
                case PlayerList.ChangeType.Change:
                default:
                    await PlayNewFile(map);
                    break;
            }
        }

        private void ClearHitsoundPlayer()
        {
            Services.Get<PlayersInst>()?.ClearAudioPlayer();
            _forcePaused = false;
        }

        /// <summary>
        /// Initialize default player settings.
        /// </summary>
        private void LoadSurfaceSettings()
        {
            MasterVolume.Value = AppSettings.Current.Volume.Main * 100;
            MusicVolume.Value = AppSettings.Current.Volume.Music * 100;
            HitsoundVolume.Value = AppSettings.Current.Volume.Hitsound * 100;
        }

        #region Event handler

        private void ThumbButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(OnThumbClickEvent, this));
        }

        private async void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            await PlayPrev();
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            TogglePlay();
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await PlayNext();
        }

        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenNew();
        }

        /// <summary>
        /// Play progress control.
        /// While drag started, slider's updating should be paused.
        /// </summary>
        private void PlayProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            _scrollLock = true;
        }

        /// <summary>
        /// Play progress control.
        /// While drag ended, slider's updating should be recoverd.
        /// </summary>
        private void PlayProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            var progress = (int)PlayProgress.Value;
            var args = new DragCompleteEventArgs(ComponentPlayer.Current.PlayerStatus, progress);
            OnProgressDragComplete?.Invoke(this, args);
            if (!args.Handled)
            {
                switch (ComponentPlayer.Current.PlayerStatus)
                {
                    case PlayerStatus.Playing:
                        ComponentPlayer.Current.SetTime(progress, false);
                        ComponentPlayer.Current.Play();
                        break;
                    case PlayerStatus.Paused:
                    case PlayerStatus.Stopped:
                        _forcePaused = true;
                        ComponentPlayer.Current.SetTime(progress, false);
                        ComponentPlayer.Current.Pause();
                        break;
                }
            }

            _scrollLock = false;
        }

        private void ModeButton_Click(object sender, RoutedEventArgs e)
        {
            PopMode.IsOpen = true;
        }

        private void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(OnLikeClickEvent, this));
        }

        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            Pop.IsOpen = true;
        }

        private void PlayMode_Checked(object sender, RoutedEventArgs e)
        {
            var btn = (ToggleButton)sender;
            _modeOptionContainer.Switch(btn);
        }

        private async void PlayMode_Click(object sender, RoutedEventArgs e)
        {
            var btn = (ToggleButton)sender;
            PlayerMode playMode;
            switch (btn.Name)
            {
                case "Single":
                    //BtnMode.Content = "单曲播放";
                    playMode = PlayerMode.Single;
                    break;
                case "SingleLoop":
                    //BtnMode.Content = "单曲循环";
                    playMode = PlayerMode.SingleLoop;
                    break;
                case "Normal":
                    //BtnMode.Content = "顺序播放";
                    playMode = PlayerMode.Normal;
                    break;
                case "Random":
                    //BtnMode.Content = "随机播放";
                    playMode = PlayerMode.Random;
                    break;
                case "Loop":
                    //BtnMode.Content = "循环列表";
                    playMode = PlayerMode.Loop;
                    break;
                case "LoopRandom":
                default:
                    //BtnMode.Content = "随机循环";
                    playMode = PlayerMode.LoopRandom;
                    break;
            }

            await SetPlayMode(playMode);
            PopMode.IsOpen = false;
        }

        /// <summary>
        /// Master Volume Settings
        /// </summary>
        private void MasterVolume_DragComplete(object sender, DragCompletedEventArgs e)
        {
            AppSettings.SaveCurrent();
        }

        /// <summary>
        /// Music Volume Settings
        /// </summary>
        private void MusicVolume_DragComplete(object sender, DragCompletedEventArgs e)
        {
            AppSettings.SaveCurrent();
        }

        /// <summary>
        /// Effect Volume Settings
        /// </summary>
        private void HitsoundVolume_DragComplete(object sender, DragCompletedEventArgs e)
        {
            AppSettings.SaveCurrent();
        }

        /// <summary>
        /// Sample Volume Settings
        /// </summary>
        private void SampleVolume_DragComplete(object sender, DragCompletedEventArgs e)
        {
            AppSettings.SaveCurrent();
        }

        /// <summary>
        /// Balance Settings
        /// </summary>
        private void Balance_DragComplete(object sender, DragCompletedEventArgs e)
        {
            AppSettings.SaveCurrent();
        }

        /// <summary>
        /// Offset Settings
        /// </summary>
        private void Offset_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (ComponentPlayer.Current == null)
                return;
            ComponentPlayer.Current.HitsoundOffset = (int)Offset.Value;
            _appDbOperator.UpdateMap(Services.Get<PlayerList>().CurrentInfo.Identity,
                ComponentPlayer.Current.HitsoundOffset);
        }

        #endregion

        public void Dispose()
        {
            ClearHitsoundPlayer();
        }
    }

    public class DragCompleteEventArgs : HandledEventArgs
    {
        public DragCompleteEventArgs(PlayerStatus playerStatus)
        {
            PlayerStatus = playerStatus;
        }

        public DragCompleteEventArgs(PlayerStatus playerStatus, int currentPlayTime)
        {
            PlayerStatus = playerStatus;
            CurrentPlayTime = currentPlayTime;
        }

        public PlayerStatus PlayerStatus { get; }
        public int CurrentPlayTime { get; }
    }

    internal class MyCancellationTokenSource : CancellationTokenSource
    {
        public Guid Guid { get; }

        public MyCancellationTokenSource()
        {
            Guid = Guid.NewGuid();
        }
    }
}