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
    public class PlayControllerVm : WpfApi.ViewModelBase
    {
        public ObservablePlayController PlayerList { get; } = Services.Get<ObservablePlayController>();

        private PlayerViewModel _player = PlayerViewModel.Current;

        public PlayerViewModel Player
        {
            get => _player;
            set
            {
                _player = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// PlayController.xaml 的交互逻辑
    /// </summary>
    public partial class PlayController : UserControl, IDisposable
    {
        //public static PlayController Default { get; private set; }

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

        private readonly AppDbOperator _appDbOperator = new AppDbOperator();

        private readonly OptionContainer _modeOptionContainer = new OptionContainer();

        private bool _forcePaused;
        private bool _scrollLock;

        public event Action OnPlayClick;
        public event Action OnPauseClick;

        private readonly ObservablePlayController _controller = Services.Get<ObservablePlayController>();

        public PlayController()
        {
            InitializeComponent();
            PlayModeControl.CloseRequested += (sender, e) => { PopMode.IsOpen = false; };
        }

        public async Task PlayNewFile(Beatmap map, bool play = true)
        {
            string path = map.InOwnDb
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
            if (!_controller.PlayList.HasCurrent)
            {
                OpenButton_Click(null, null);
                return;
            }

            switch (_controller.Player.PlayStatus)
            {
                case PlayStatus.Playing:
                    {
                        var args = new RoutedEventArgs(PreviewPauseEvent, this);
                        RaiseEvent(args);
                        if (!args.Handled)
                        {
                            PauseAudio();
                        }

                        break;
                    }
                case PlayStatus.Ready:
                case PlayStatus.Stopped:
                case PlayStatus.Paused:
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
        }

        public async Task SetPlayMode(PlayMode playMode)
        {
            _controller.PlayList.PlayMode = playMode;
            AppSettings.Default.Play.PlayMode = playMode;
            AppSettings.SaveDefault();
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

            var dbInst = Services.Get<OsuDbInst>();
            ComponentPlayer audioPlayer = null;

            if (path == null)
                return;
            if (File.Exists(path))
            {
                try
                {
                    var osuFile = await OsuFile.ReadFromFileAsync(path, options => options.ExcludeSection("Editor")); //50 ms
                    var fi = new FileInfo(path);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot locate.", fi.FullName);
                    var dir = fi.Directory.FullName;

                    /* Clear */
                    ClearHitsoundPlayer();
                    if (System.IO.Path.GetDirectoryName(AppSettings.Default.CurrentMap) !=
                        System.IO.Path.GetDirectoryName(path))
                    {
                        Services.Get<PlayersInst>()?.ClearHitsoundCache();
                    }

                    /* Set Meta */
                    var nowIdentity = new MapIdentity(fi.Directory.Name, osuFile.Metadata.Version);

                    BeatmapSettings beatmapSettings = _appDbOperator.GetMapFromDb(nowIdentity);
                    Beatmap beatmap = _appDbOperator.GetBeatmapByIdentifiable(nowIdentity);

                    bool isFavorite = IsMapFavorite(beatmapSettings); //50 ms

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
                    LblNowFake.Visibility = Visibility.Visible;
                    LblTotalFake.Visibility = Visibility.Visible;
                    PlayProgress.Maximum = 1;
                    PlayProgress.Value = 0;

                    /* Set Thumb */
                    var defaultPath = System.IO.Path.Combine(Domain.ResourcePath, "default.jpg");
                    string truePath;
                    if (osuFile.Events.BackgroundInfo != null)
                    {
                        var bgPath = System.IO.Path.Combine(dir, osuFile.Events.BackgroundInfo.Filename);
                        truePath = File.Exists(bgPath)
                            ? bgPath
                            : (File.Exists(defaultPath)
                                ? defaultPath
                                : null);
                    }
                    else
                    {
                        truePath = File.Exists(defaultPath)
                            ? defaultPath
                            : null;
                    }

                    Thumb.Source = truePath == null ? null : new BitmapImage(new Uri(truePath));
                    /* Set new hitsound player*/
                    playerInst.SetAudioPlayer(path, osuFile);
                    audioPlayer = playerInst.AudioPlayer;
                    SignUpPlayerEvent(audioPlayer);
                    await audioPlayer.InitializeAsync(); //700 ms
                    audioPlayer.HitsoundOffset = beatmapSettings.Offset;
                    VolumeControl.HitsoundOffset = audioPlayer.HitsoundOffset;

                    var currentInfo = new BeatmapDetail(
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
                        beatmapSettings,
                        beatmap,
                        isFavorite, path, truePath); // 20 ms
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
                    OnNewFileLoaded?.Invoke(play, args);
                    if (!args.Handled)
                    {
                        if (play)
                        {
                            audioPlayer.Play();
                        }
                    }

                    LblNow.Visibility = Visibility.Visible;
                    LblTotal.Visibility = Visibility.Visible;
                    LblNowFake.Visibility = Visibility.Hidden;
                    LblTotalFake.Visibility = Visibility.Hidden;
                    AppSettings.Default.CurrentMap = path;
                    AppSettings.SaveDefault();

                    _appDbOperator.UpdateMap(nowIdentity);
                }
                catch (Exception ex)
                {
                    OsuPlayer.Notification.Show(@"发生未处理的错误：" + (ex.InnerException?.Message ?? ex?.Message));

                    if (audioPlayer == null) return;
                    if (audioPlayer.PlayStatus != PlayStatus.Playing)
                    {
                        await PlayNextAsync(false, true);
                    }
                }
            }
            else
            {
                OsuPlayer.Notification.Show(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
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

        private bool IsMapFavorite(BeatmapSettings settings)
        {
            var album = _appDbOperator.GetCollectionsByMap(settings);
            bool isFavorite = album != null && album.Any(k => k.LockedBool);

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
            var args = new DragCompleteEventArgs(ComponentPlayer.Current.PlayStatus, progress);
            OnProgressDragComplete?.Invoke(this, args);
            if (!args.Handled)
            {
                switch (ComponentPlayer.Current.PlayStatus)
                {
                    case PlayStatus.Playing:
                        ComponentPlayer.Current.SetTime(progress, false);
                        ComponentPlayer.Current.Play();
                        break;
                    case PlayStatus.Paused:
                    case PlayStatus.Stopped:
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

        private void PlayListButton_Click(object sender, RoutedEventArgs e)
        {
            PopPlayList.IsOpen = true;
        }

        #endregion

        public void Dispose()
        {
            ClearHitsoundPlayer();
        }

        private void PlayListControl_CloseRequested(object sender, RoutedEventArgs e)
        {
            PopPlayList.IsOpen = false;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var playList = Services.Get<PlayerList>();
            await SetPlayMode(playList.PlayerMode);
        }

        private void TitleArtist_Click(object sender, RoutedEventArgs e)
        {
            var playerList = Services.Get<PlayerList>();
            var win = new BeatmapInfoWindow(playerList.CurrentInfo);
            win.ShowDialog();
        }
    }

    public class DragCompleteEventArgs : HandledEventArgs
    {
        public DragCompleteEventArgs(PlayStatus playStatus)
        {
            PlayStatus = playStatus;
        }

        public DragCompleteEventArgs(PlayStatus playStatus, int currentPlayTime)
        {
            PlayStatus = playStatus;
            CurrentPlayTime = currentPlayTime;
        }

        public PlayStatus PlayStatus { get; }
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