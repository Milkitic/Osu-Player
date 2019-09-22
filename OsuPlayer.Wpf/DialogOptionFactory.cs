using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Control.FrontDialog;

namespace Milky.OsuPlayer
{
    class DialogOptionFactory
    {
        public static FrontDialogOverlay.ShowContentOptions DiffSelectOptions => new FrontDialogOverlay.ShowContentOptions
        {
            Title = "选择难度",
            ShowDialogButtons = false,
            Width = 300,
            Height = 400
        };

        public static FrontDialogOverlay.ShowContentOptions SelectCollectionOptions => new FrontDialogOverlay.ShowContentOptions
        {
            Title = "选择收藏",
            ShowDialogButtons = false,
            Width = 300,
            Height = 400
        };

        public static FrontDialogOverlay.ShowContentOptions AddCollectionOptions => new FrontDialogOverlay.ShowContentOptions
        {
            Title = "新建收藏",
            ShowDialogButtons = true,
            Width = 290,
            Height = 155
        };
        public static FrontDialogOverlay.ShowContentOptions EditCollectionOptions => new FrontDialogOverlay.ShowContentOptions
        {
            Title = "编辑收藏",
            ShowDialogButtons = false,
            Width = 600,
            Height = 285
        };
    }
}
