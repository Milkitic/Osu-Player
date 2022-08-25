using Milki.OsuPlayer.Utils;
using static Milki.OsuPlayer.UiComponents.ContentDialogComponent.ContentDialog;

namespace Milki.OsuPlayer.UiComponents.ContentDialogComponent;

internal static class DialogOptionFactory
{
    public static DialogOptions DiffSelectOptions => new()
    {
        Title = I18NUtil.GetString("ui-win-selectDifficulty"),
        ShowDialogButtons = false,
        Width = 300,
        Height = 300
    };

    public static DialogOptions SelectPlayListOptions => new()
    {
        Title = I18NUtil.GetString("ui-win-selectCollection"),
        ShowDialogButtons = false,
        Width = 300,
        Height = 400
    };

    public static DialogOptions AddCollectionOptions => new()
    {
        Title = I18NUtil.GetString("ui-win-newCollection"),
        ShowDialogButtons = true,
        Width = 290,
        Height = 135
    };

    public static DialogOptions EditPlayListOptions => new()
    {
        Title = I18NUtil.GetString("ui-win-editCollection"),
        ShowDialogButtons = false,
        Width = 600,
        Height = 285
    };

    public static DialogOptions ClosingOptions => new()
    {
        Width = 280,
        Height = 180,
        Title = I18NUtil.GetString("ui-win-closing")
    };
}