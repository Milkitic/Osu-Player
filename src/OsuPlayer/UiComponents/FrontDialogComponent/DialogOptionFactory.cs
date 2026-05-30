using Milky.OsuPlayer.Utils;

namespace Milky.OsuPlayer.UiComponents.FrontDialogComponent
{
    internal class DialogOptionFactory
    {
        public static FrontDialogOverlay.ShowContentOptions DiffSelectOptions => new FrontDialogOverlay.ShowContentOptions
        {
            Title = I18NUtil.GetString("ui-win-selectDifficulty"),
            ShowDialogButtons = false,
            Width = 300,
            Height = 400
        };

        public static FrontDialogOverlay.ShowContentOptions SelectCollectionOptions => new FrontDialogOverlay.ShowContentOptions
        {
            Title = I18NUtil.GetString("ui-win-selectCollection"),
            ShowDialogButtons = false,
            Width = 300,
            Height = 400
        };

        public static FrontDialogOverlay.ShowContentOptions AddCollectionOptions => new FrontDialogOverlay.ShowContentOptions
        {
            Title = I18NUtil.GetString("ui-win-newCollection"),
            ShowDialogButtons = true,
            Width = 290,
            Height = 155
        };
        public static FrontDialogOverlay.ShowContentOptions EditCollectionOptions => new FrontDialogOverlay.ShowContentOptions
        {
            Title = I18NUtil.GetString("ui-win-editCollection"),
            ShowDialogButtons = false,
            Width = 600,
            Height = 285
        };

        public static FrontDialogOverlay.ShowContentOptions ClosingOptions => new FrontDialogOverlay.ShowContentOptions
        {
            Width = 280,
            Height = 180,
            Title = I18NUtil.GetString("ui-win-closing")
        };
    }
}
