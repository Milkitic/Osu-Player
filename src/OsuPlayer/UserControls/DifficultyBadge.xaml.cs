using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Coosu.Beatmap.Sections.GamePlay;

namespace Milky.OsuPlayer.UserControls;

public partial class DifficultyBadge : UserControl
{
    public static readonly DependencyProperty GameModeProperty = DependencyProperty.Register(
        nameof(GameMode), typeof(GameMode), typeof(DifficultyBadge), new PropertyMetadata(GameMode.Circle));
    public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(
        nameof(Version), typeof(string), typeof(DifficultyBadge), new PropertyMetadata("version"));
    public static readonly DependencyProperty StarRatingProperty = DependencyProperty.Register(
        nameof(StarRating), typeof(double), typeof(DifficultyBadge), new PropertyMetadata(default(double)));
    public static readonly DependencyProperty VersionForegroundProperty = DependencyProperty.Register(
        nameof(VersionForeground), typeof(Brush), typeof(DifficultyBadge), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x24, 0x24, 0x24))));

    public DifficultyBadge()
    {
        InitializeComponent();
    }

    public GameMode GameMode
    {
        get => (GameMode)GetValue(GameModeProperty);
        set => SetValue(GameModeProperty, value);
    }

    public string Version
    {
        get => (string)GetValue(VersionProperty);
        set => SetValue(VersionProperty, value);
    }

    public double StarRating
    {
        get => (double)GetValue(StarRatingProperty);
        set => SetValue(StarRatingProperty, value);
    }

    public Brush VersionForeground
    {
        get => (Brush)GetValue(VersionForegroundProperty);
        set => SetValue(VersionForegroundProperty, value);
    }
}