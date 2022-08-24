using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Coosu.Beatmap.Sections.GamePlay;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Pages;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.UiComponents.NotificationComponent;

namespace Milki.OsuPlayer.UserControls;

/// <summary>
/// CardControl.xaml 的交互逻辑
/// </summary>
public partial class CardControl : UserControl
{
    public static readonly DependencyProperty ThumbPathProperty = DependencyProperty.Register(
        nameof(ThumbPath), typeof(string), typeof(CardControl), new PropertyMetadata("pack://application:,,,/OsuPlayer;component/Resources/official/registration.jpg"));
    public static readonly DependencyProperty ArtistProperty = DependencyProperty.Register(
        nameof(Artist), typeof(string), typeof(CardControl), new PropertyMetadata("artist"));
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(CardControl), new PropertyMetadata("title"));
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(string), typeof(CardControl), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty CreatorProperty = DependencyProperty.Register(
        nameof(Creator), typeof(string), typeof(CardControl), new PropertyMetadata("cretor"));
    public static readonly DependencyProperty GroupPlayItemsProperty = DependencyProperty.Register(
        nameof(GroupPlayItems), typeof(Dictionary<GameMode, PlayItem[]>), typeof(CardControl), new PropertyMetadata(default(Dictionary<GameMode, PlayItem[]>)));
    public static readonly DependencyProperty PlayItemProperty = DependencyProperty.Register(
        nameof(PlayItem), typeof(PlayItem), typeof(CardControl), new PropertyMetadata(default(PlayItem)));

    private readonly PlayerService _playerService;

    public CardControl()
    {
        InitializeComponent();
        _playerService = App.Current.ServiceProvider.GetService<PlayerService>();
    }

    public string ThumbPath
    {
        get => (string)GetValue(ThumbPathProperty);
        set => SetValue(ThumbPathProperty, value);
    }

    public string Artist
    {
        get => (string)GetValue(ArtistProperty);
        set => SetValue(ArtistProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string Creator
    {
        get => (string)GetValue(CreatorProperty);
        set => SetValue(CreatorProperty, value);
    }

    public Dictionary<GameMode, PlayItem[]> GroupPlayItems
    {
        get => (Dictionary<GameMode, PlayItem[]>)GetValue(GroupPlayItemsProperty);
        set => SetValue(GroupPlayItemsProperty, value);
    }

    public PlayItem PlayItem
    {
        get => (PlayItem)GetValue(PlayItemProperty);
        set => SetValue(PlayItemProperty, value);
    }

    private async void BtnPlayDefault_OnClick(object sender, RoutedEventArgs e)
    {
        await _playerService.InitializeNewAsync(PlayItem.StandardizedPath, true);
    }

    private async void MiPlay_OnClick(object sender, RoutedEventArgs e)
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var playItems = await dbContext.GetPlayItemsByFolderAsync(PlayItem.StandardizedFolder);
        var control = new DiffSelectControl(playItems, async (selected, arg) =>
        {
            await _playerService.InitializeNewAsync(selected.StandardizedPath, true);
        });
       App.Current.MainWindow.ContentDialog.ShowContent(control, DialogOptionFactory.DiffSelectOptions);
    }

    private void MiSearchTitle_OnClick(object sender, RoutedEventArgs e)
    {
        App.Current.MainWindow.NavigationBar.SwitchSearch
            .CheckAndAction(page => ((SearchPage)page).Search(Title));
    }

    private void MiSearchArtist_OnClick(object sender, RoutedEventArgs e)
    {
        App.Current.MainWindow.NavigationBar.SwitchSearch
            .CheckAndAction(page => ((SearchPage)page).Search(Artist));
    }

    private void MiSearchSource_OnClick(object sender, RoutedEventArgs e)
    {
        App.Current.MainWindow.NavigationBar.SwitchSearch
            .CheckAndAction(page => ((SearchPage)page).Search(Source));
    }

    private void MiSearchCreator_OnClick(object sender, RoutedEventArgs e)
    {
        App.Current.MainWindow.NavigationBar.SwitchSearch
            .CheckAndAction(page => ((SearchPage)page).Search(Creator));
    }

    private void MiOpenFolder_OnClick(object sender, RoutedEventArgs e)
    {
        var standardizedFolder = PlayItem.StandardizedFolder;
        var folder = PathUtils.GetFullPath(standardizedFolder, AppSettings.Default.GeneralSection.OsuSongDir);
        if (!Directory.Exists(folder))
        {
            Notification.Push(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
            return;
        }

        Process.Start(new ProcessStartInfo(folder)
        {
            UseShellExecute = true
        });
    }

    private void MiOpenScorePage_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void MiPlayList_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void MiExport_OnClick(object sender, RoutedEventArgs e)
    {
    }
}