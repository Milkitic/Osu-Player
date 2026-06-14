using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OsuPlayer.Core;

namespace OsuPlayer.Views.UserControls;

public partial class BeatmapCard : UserControl
{
    public static readonly StyledProperty<ICommand?> DirectPlayCommandProperty =
        AvaloniaProperty.Register<BeatmapCard, ICommand?>(nameof(DirectPlayCommand));

    public static readonly StyledProperty<ICommand?> PlayCommandProperty =
        AvaloniaProperty.Register<BeatmapCard, ICommand?>(nameof(PlayCommand));

    public static readonly StyledProperty<ICommand?> SearchByConditionCommandProperty =
        AvaloniaProperty.Register<BeatmapCard, ICommand?>(nameof(SearchByConditionCommand));

    public static readonly StyledProperty<ICommand?> OpenSourceFolderCommandProperty =
        AvaloniaProperty.Register<BeatmapCard, ICommand?>(nameof(OpenSourceFolderCommand));

    public static readonly StyledProperty<ICommand?> OpenScorePageCommandProperty =
        AvaloniaProperty.Register<BeatmapCard, ICommand?>(nameof(OpenScorePageCommand));

    public static readonly StyledProperty<ICommand?> SaveCollectionCommandProperty =
        AvaloniaProperty.Register<BeatmapCard, ICommand?>(nameof(SaveCollectionCommand));

    public static readonly StyledProperty<ICommand?> ExportCommandProperty =
        AvaloniaProperty.Register<BeatmapCard, ICommand?>(nameof(ExportCommand));

    public static readonly StyledProperty<ICommand?> RemoveCommandProperty =
        AvaloniaProperty.Register<BeatmapCard, ICommand?>(nameof(RemoveCommand));

    public static readonly StyledProperty<bool> ShowRemoveProperty =
        AvaloniaProperty.Register<BeatmapCard, bool>(nameof(ShowRemove));

    public static readonly StyledProperty<bool> ShowDifficultyOverlayProperty =
        AvaloniaProperty.Register<BeatmapCard, bool>(nameof(ShowDifficultyOverlay));

    public ICommand? DirectPlayCommand
    {
        get => GetValue(DirectPlayCommandProperty);
        set => SetValue(DirectPlayCommandProperty, value);
    }

    public ICommand? PlayCommand
    {
        get => GetValue(PlayCommandProperty);
        set => SetValue(PlayCommandProperty, value);
    }

    public ICommand? SearchByConditionCommand
    {
        get => GetValue(SearchByConditionCommandProperty);
        set => SetValue(SearchByConditionCommandProperty, value);
    }

    public ICommand? OpenSourceFolderCommand
    {
        get => GetValue(OpenSourceFolderCommandProperty);
        set => SetValue(OpenSourceFolderCommandProperty, value);
    }

    public ICommand? OpenScorePageCommand
    {
        get => GetValue(OpenScorePageCommandProperty);
        set => SetValue(OpenScorePageCommandProperty, value);
    }

    public ICommand? SaveCollectionCommand
    {
        get => GetValue(SaveCollectionCommandProperty);
        set => SetValue(SaveCollectionCommandProperty, value);
    }

    public ICommand? ExportCommand
    {
        get => GetValue(ExportCommandProperty);
        set => SetValue(ExportCommandProperty, value);
    }

    public ICommand? RemoveCommand
    {
        get => GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    public bool ShowRemove
    {
        get => GetValue(ShowRemoveProperty);
        set => SetValue(ShowRemoveProperty, value);
    }

    public bool ShowDifficultyOverlay
    {
        get => GetValue(ShowDifficultyOverlayProperty);
        set => SetValue(ShowDifficultyOverlayProperty, value);
    }

    public BeatmapCard()
    {
        InitializeComponent();
        UpdateRemoveVisibility();
    }

    private BeatmapDataModel? Beatmap => DataContext as BeatmapDataModel;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShowRemoveProperty)
        {
            UpdateRemoveVisibility();
        }
    }

    private void Card_Click(object? sender, RoutedEventArgs e)
    {
        Execute(DirectPlayCommand, Beatmap);
    }

    private void PlayWithDifficulty_Click(object? sender, RoutedEventArgs e)
    {
        Execute(PlayCommand, Beatmap);
    }

    private void SearchByCondition_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: string field } || Beatmap is not { } beatmap)
        {
            return;
        }

        var keyword = field switch
        {
            "Title" => beatmap.AutoTitle,
            "Artist" => beatmap.AutoArtist,
            "Source" => beatmap.SongSource,
            "Creator" => beatmap.Creator,
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            Execute(SearchByConditionCommand, keyword);
        }
    }

    private void OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        Execute(OpenSourceFolderCommand, Beatmap);
    }

    private void OpenScorePage_Click(object? sender, RoutedEventArgs e)
    {
        Execute(OpenScorePageCommand, Beatmap);
    }

    private void SaveCollection_Click(object? sender, RoutedEventArgs e)
    {
        Execute(SaveCollectionCommand, Beatmap);
    }

    private void Export_Click(object? sender, RoutedEventArgs e)
    {
        Execute(ExportCommand, Beatmap);
    }

    private void Remove_Click(object? sender, RoutedEventArgs e)
    {
        Execute(RemoveCommand, Beatmap);
    }

    private static void Execute(ICommand? command, object? parameter)
    {
        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
        }
    }

    private void UpdateRemoveVisibility()
    {
        RemoveSeparator.IsVisible = ShowRemove;
        RemoveMenuItem.IsVisible = ShowRemove;
    }
}
