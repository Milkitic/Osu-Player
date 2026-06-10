using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Coosu.Beatmap.Sections.GamePlay;

namespace OsuPlayer.Views.UserControls;

public partial class DifficultyBadge : UserControl
{
    public static readonly StyledProperty<GameMode> GameModeProperty =
        AvaloniaProperty.Register<DifficultyBadge, GameMode>(nameof(GameMode), GameMode.Circle);

    public static readonly StyledProperty<string> VersionProperty =
        AvaloniaProperty.Register<DifficultyBadge, string>(nameof(Version), "version");

    public static readonly StyledProperty<double> StarRatingProperty =
        AvaloniaProperty.Register<DifficultyBadge, double>(nameof(StarRating), 0.0);

    public static readonly StyledProperty<IBrush?> VersionForegroundProperty =
        AvaloniaProperty.Register<DifficultyBadge, IBrush?>(nameof(VersionForeground), new SolidColorBrush(Color.FromRgb(0x24, 0x24, 0x24)));

    public GameMode GameMode
    {
        get => GetValue(GameModeProperty);
        set => SetValue(GameModeProperty, value);
    }

    public string Version
    {
        get => GetValue(VersionProperty);
        set => SetValue(VersionProperty, value);
    }

    public double StarRating
    {
        get => GetValue(StarRatingProperty);
        set => SetValue(StarRatingProperty, value);
    }

    public IBrush? VersionForeground
    {
        get => GetValue(VersionForegroundProperty);
        set => SetValue(VersionForegroundProperty, value);
    }

    public DifficultyBadge()
    {
        InitializeComponent();
    }
}
