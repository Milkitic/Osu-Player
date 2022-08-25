using System.Windows;
using System.Windows.Controls;
using Coosu.Beatmap.Sections.GamePlay;

namespace Milki.OsuPlayer.UserControls;

/// <summary>
/// DifficultyBadge.xaml 的交互逻辑
/// </summary>
public partial class DifficultyBadge : UserControl
{
    public static readonly DependencyProperty GameModeProperty = DependencyProperty.Register(
        nameof(GameMode), typeof(GameMode), typeof(DifficultyBadge), new PropertyMetadata(GameMode.Circle));
    public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(
        nameof(Version), typeof(string), typeof(DifficultyBadge), new PropertyMetadata("version"));
    public static readonly DependencyProperty StarRatingProperty = DependencyProperty.Register(
        nameof(StarRating), typeof(double), typeof(DifficultyBadge), new PropertyMetadata(default(double)));
    public static readonly DependencyProperty BadgeSizeProperty = DependencyProperty.Register(
        nameof(BadgeSize), typeof(BadgeSize), typeof(DifficultyBadge), new PropertyMetadata(BadgeSize.Regular));

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

    public BadgeSize BadgeSize
    {
        get => (BadgeSize)GetValue(BadgeSizeProperty);
        set => SetValue(BadgeSizeProperty, value);
    }
}

public enum BadgeSize
{
    Regular, Small
}