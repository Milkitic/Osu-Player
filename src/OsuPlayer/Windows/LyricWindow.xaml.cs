using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Anotar.NLog;
using Coosu.Beatmap;
using LyricsFinder;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.ViewModels;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Windows;

/// <summary>
/// LyricWindow.xaml 的交互逻辑
/// </summary>
public partial class LyricWindow : WindowEx
{
    //动画定义
    private Storyboard _myStoryboard;
    private Timer _frameTimer;

    private readonly LyricWindowViewModel _viewModel;
    private readonly PlayerService _playerService;
    private readonly LyricsService _lyricsService;

    private List<Sentence> _lyricList;
    private CancellationTokenSource _cts;
    private Task _playingTask;
    private bool _pressed;

    public LyricWindow()
    {
        _playerService = ServiceProviders.Default.GetService<PlayerService>();
        _lyricsService = ServiceProviders.Default.GetService<LyricsService>();
        Loaded += WindowBase_Loaded;

        InitializeComponent();
        DataContext = _viewModel = new LyricWindowViewModel();

        CompositionTarget.Rendering += OnRendering;
        Left = 0;
        Top = SystemParameters.WorkArea.Height - Height - 20;
        Width = SystemParameters.PrimaryScreenWidth;
        MouseMove += LyricWindow_MouseMove;
        MouseLeave += LyricWindow_MouseLeave;
    }

    public async ValueTask StopTaskAsync()
    {
        await CancelTask();
    }

    public async ValueTask ShowAsync()
    {
        base.Show();
        var lastLoadContext = _playerService.LastLoadContext;
        var meta = lastLoadContext?.OsuFile?.Metadata;
        MetaString metaArtist = meta?.ArtistMeta ?? default;
        MetaString metaTitle = meta?.TitleMeta ?? default;
        await SetNewLyric(null, metaArtist, metaTitle);
        AppSettings.Default.LyricSection.IsDesktopLyricEnabled = true;
        AppSettings.SaveDefault();

        SharedVm.Default.IsLyricWindowEnabled = true;
        _lyricsService.SetLyricSynchronously(lastLoadContext?.PlayItem);
    }

    public async ValueTask HideAsync()
    {
        base.Hide();
        AppSettings.Default.LyricSection.IsDesktopLyricEnabled = false;
        AppSettings.SaveDefault();
        SharedVm.Default.IsLyricWindowEnabled = false;
        await CancelTask();
    }

    public async ValueTask SetNewLyric(Lyrics lyric, MetaString metaArtist, MetaString metaTitle)
    {
        await StopTaskAsync();

        _lyricList = lyric?.LyricsSentences ?? new List<Sentence>();
        _lyricList.Insert(0,
            new Sentence($"{metaArtist.ToUnicodeString()} - {metaTitle.ToUnicodeString()}", 0));
    }

    public void StartWork()
    {
        _cts = new CancellationTokenSource();
        _playingTask = Task.Run(() =>
        {
            int oldTime = -1;
            while (!_cts.Token.IsCancellationRequested)
            {
                Thread.Sleep(50);
                var currentTime = _playerService.PlayTime;
                var validLyrics = _lyricList.Where(t => t.StartTime <= currentTime.TotalMilliseconds).ToArray();
                if (validLyrics.Length < 1)
                    continue;
                int maxTime = validLyrics.Max(t => t.StartTime);
                if (oldTime == maxTime)
                    continue;
                var current = _lyricList.First(t => t.StartTime == maxTime);
                var predictLyrics = _lyricList.Where(t => t.StartTime > maxTime);
                Sentence? next = null;
                if (predictLyrics.Any())
                    next = _lyricList.First(t => t.StartTime > maxTime);
                LogTo.Debug(() => current.Content);

                var size = SetLyricByIndexAsync(_lyricList.IndexOf(current)).AsTask().Result;
                Execute.OnUiThread(() => { BeginTranslate(size, maxTime, next?.StartTime ?? -1); });
                _pressed = false;
                oldTime = maxTime;
            }
        }, _cts.Token);
    }

    public void Dispose()
    {
        StopTaskAsync().AsTask().Wait();
        Close();
    }

    private void LyricWindow_Loaded(object sender, RoutedEventArgs e)
    {
        FontFamily lyricFont;
        if (AppSettings.Default.LyricSection.FontFamily == null)
        {
            lyricFont = Application.Current.FindResource("GenericRegular") as FontFamily;
        }
        else
        {
            lyricFont = new FontFamily(AppSettings.Default.LyricSection.FontFamily);
        }

        _viewModel.FontFamily = lyricFont;
        _viewModel.Hue = AppSettings.Default.LyricSection.AppearanceHue;
        _viewModel.Saturation = AppSettings.Default.LyricSection.AppearanceSaturation;
    }

    private void LyricWindow_MouseMove(object sender, MouseEventArgs e)
    {
        _frameTimer?.Dispose();
        _viewModel.ShowFrame = true;
    }

    private void LyricWindow_MouseLeave(object sender, MouseEventArgs e)
    {
        _frameTimer = new Timer(_ =>
        {
            Execute.OnUiThread(() => _viewModel.ShowFrame = false);
        }, null, 1500, Timeout.Infinite);
    }

    private void OnRendering(object sender, EventArgs e)
    {
        if (!_pressed)
            Left = 0;
    }

    private void BeginTranslate(Size size, int nowTime, int nextTime)
    {
        _myStoryboard?.Stop();
        _myStoryboard?.Remove();
        LyricBar.ClearValue(Border.MarginProperty);
        double viewWidth = CutView.MaxWidth, width = size.Width;
        if (width <= viewWidth) return;

        LogTo.Debug(() => $@"{size.Width}>{viewWidth}");

        //const double minInterval = 0.5;
        //if (nextTime - nowTime < minInterval) return;
        var interval = nextTime == -1 ? 4000 : (nextTime - nowTime);
        double startTime = interval / 5 > 3000 ? 3000 : interval / 5;
        double duration;
        if (nextTime == -1)
            duration = 3000;
        else
        {
            if (nextTime - nowTime < 10000)
            {
                if (interval - startTime < 1000)
                {
                    duration = interval - startTime;
                }
                else
                {
                    duration = interval - startTime - 1000;
                }
            }
            else
            {
                duration = 10000 - startTime - 1000;
            }
        }

        LogTo.Debug(() => $"{0}->{viewWidth - width}, start: {startTime}, duration: {duration}");
        var defaultAnimation = new ThicknessAnimation
        {
            From = new Thickness(0),
            To = new Thickness(0),
            BeginTime = TimeSpan.FromMilliseconds(0),
            Duration = /*CommonUtils.GetDuration*/(TimeSpan.FromMilliseconds(startTime))
        };
        var translateAnimation = new ThicknessAnimation
        {
            From = new Thickness(0),
            To = new Thickness(viewWidth - width - 16, 0, 0, 0),
            BeginTime = TimeSpan.FromMilliseconds(startTime),
            Duration = /*CommonUtils.GetDuration*/(TimeSpan.FromMilliseconds(duration))
        };

        Storyboard.SetTarget(defaultAnimation, LyricBar);
        Storyboard.SetTarget(translateAnimation, LyricBar);

        Storyboard.SetTargetProperty(defaultAnimation, new PropertyPath(Border.MarginProperty));
        Storyboard.SetTargetProperty(translateAnimation, new PropertyPath(Border.MarginProperty));

        _myStoryboard = new Storyboard();
        _myStoryboard.Children.Add(defaultAnimation);
        _myStoryboard.Children.Add(translateAnimation);

        _myStoryboard.Begin();
    }

    private async ValueTask<Size> SetLyricByIndexAsync(int index)
    {
        var content = _lyricList[index].Content;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var tcs = new TaskCompletionSource<Size>();
        cts.Token.Register(_ => tcs.TrySetCanceled(), null);

        Execute.OnUiThread(() =>
        {
            TbLyric.FinalSizeChanged += size => tcs.TrySetResult(size);
            TbLyric.Text = content;
        });

        Size lyricSize;
        try
        {
            lyricSize = await tcs.Task;
        }
        catch (TaskCanceledException)
        {
            lyricSize = Size.Empty;
        }

        LogTo.Debug(() => lyricSize.ToString());
        return lyricSize;
    }

    private async ValueTask CancelTask()
    {
        _cts?.Cancel();
        if (_playingTask != null) await _playingTask;
    }

    private void ImgLyric_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _pressed = true;
            this.DragMove();
        }
    }

    private void ImgLyric_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _pressed = false;
    }

    private async void BtnHide_Click(object sender, RoutedEventArgs e)
    {
        await HideAsync();
    }

    private void BtnLock_Click(object sender, RoutedEventArgs e)
    {
        IsLocked = true;
    }

    private async void BtnPrev_Click(object sender, RoutedEventArgs e)
    {
        await _playerService.PlayPreviousAsync();
    }

    private async void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        await _playerService.TogglePlayAsync();
    }

    private async void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        await _playerService.PlayNextAsync();
    }

    private void BtnFont_Click(object sender, RoutedEventArgs e)
    {
        popFontFamily.IsOpen = true;
    }

    private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
    {
        lvFontFamilies.ScrollIntoView(_viewModel.FontFamily);
    }

    private void BtnPalette_Click(object sender, RoutedEventArgs e)
    {
        popHsl.IsOpen = true;
    }

    private void sldHue_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        AppSettings.Default.LyricSection.AppearanceHue = _viewModel.Hue;
        AppSettings.SaveDefault();
    }

    private void sldSaturation_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        AppSettings.Default.LyricSection.AppearanceSaturation = _viewModel.Saturation;
        AppSettings.SaveDefault();
    }

    private void sldLightness_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        AppSettings.Default.LyricSection.AppearanceLightness = _viewModel.Lightness;
        AppSettings.SaveDefault();
    }
}