using Milki.OsuPlayer.Utils;

namespace Milki.OsuPlayer.UiComponents.FrontDialogComponent;

internal class DialogOptionFactory
{
    public static FrontDialogOverlay.ShowContentOptions DiffSelectOptions => new()
    {
        Title = I18NUtil.GetString("ui-win-selectDifficulty"),
        ShowDialogButtons = false,
        Width = 300,
        Height = 200
    };

    public static FrontDialogOverlay.ShowContentOptions SelectCollectionOptions => new()
    {
        Title = I18NUtil.GetString("ui-win-selectCollection"),
        ShowDialogButtons = false,
        Width = 300,
        Height = 400
    };

    public static FrontDialogOverlay.ShowContentOptions AddCollectionOptions => new()
    {
        Title = I18NUtil.GetString("ui-win-newCollection"),
        ShowDialogButtons = true,
        Width = 290,
        Height = 155
    };
    public static FrontDialogOverlay.ShowContentOptions EditCollectionOptions => new()
    {
        Title = I18NUtil.GetString("ui-win-editCollection"),
        ShowDialogButtons = false,
        Width = 600,
        Height = 285
    };

    public static FrontDialogOverlay.ShowContentOptions ClosingOptions => new()
    {
        Width = 280,
        Height = 180,
        Title = I18NUtil.GetString("ui-win-closing")
    };
}