using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using OsuPlayer.Controls;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Lang;
using OsuPlayer.Localization;
using OsuPlayer.Views.UserControls;
using OsuPlayer.Windows;

namespace OsuPlayer.Services;

internal static class FrontDialogService
{
    internal const string MainWindowDialogIdentifier = "MainWindowDialog";

    private const double AddCollectionWidth = 290;
    private const double AddCollectionHeight = 155;
    private const double SelectCollectionWidth = 300;
    private const double SelectCollectionHeight = 400;
    private const double DiffSelectWidth = 300;
    private const double DiffSelectHeight = 400;

    public static MainWindow? GetMainWindow()
        => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as MainWindow;

    public static async Task<bool> ShowAddCollectionAsync(
        Visual? owner,
        IPlayerDataService playerData,
        Func<Task>? afterAdded = null)
    {
        var content = new AddCollectionControl();
        var dialog = CreateDialog(
            LocalizationService.Instance[SRKeys.Ui_Win_NewCollection],
            AddCollectionWidth,
            AddCollectionHeight,
            content,
            showFooter: true);

        dialog.FooterButtonStyle = FooterButtonStyle.Yes;
        dialog.FooterYesButtonText = LocalizationService.Instance[SRKeys.Ui_Ok];
        dialog.Loaded += (_, _) => Dispatcher.UIThread.Post(content.FocusCollectionName);

        var result = await ShowAsync(owner, dialog);
        if (result is not true)
        {
            return false;
        }

        var collectionName = content.CollectionNameValue;
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            content.FocusCollectionName();
            return false;
        }

        if (!await playerData.TryAddCollectionAsync(collectionName, false))
        {
            content.FocusCollectionName();
            return false;
        }

        if (afterAdded != null)
        {
            await afterAdded();
        }
        else if (GetMainWindow() is { } mainWindow)
        {
            await mainWindow.UpdateCollectionsAsync();
        }

        return true;
    }

    public static Task<bool> ShowSelectCollectionAsync(
        Visual? owner,
        Beatmap entry,
        Func<Task>? afterClosed = null)
        => ShowSelectCollectionAsync(owner, new List<Beatmap> { entry }, afterClosed);

    public static async Task<bool> ShowSelectCollectionAsync(
        Visual? owner,
        IList<Beatmap> entries,
        Func<Task>? afterClosed = null)
    {
        if (entries.Count == 0)
        {
            return false;
        }

        var content = new SelectCollectionControl(entries);
        var dialog = CreateDialog(
            LocalizationService.Instance[SRKeys.Ui_Win_SelectCollection],
            SelectCollectionWidth,
            SelectCollectionHeight,
            content,
            showFooter: false);

        content.CloseRequested += (_, _) => dialog.CloseDialogCommand.Execute(true);

        var result = await ShowAsync(owner, dialog);
        if (result is not true)
        {
            return false;
        }

        if (afterClosed != null)
        {
            await afterClosed();
        }

        return true;
    }

    public static async Task<Beatmap?> ShowDifficultyPickerAsync(Visual? owner, IReadOnlyList<Beatmap> beatmaps)
    {
        if (beatmaps.Count == 0)
        {
            return null;
        }

        Beatmap? selected = null;
        var content = new DiffSelectControl(beatmaps);
        var dialog = CreateDialog(
            LocalizationService.Instance[SRKeys.Ui_Win_SelectDifficulty],
            DiffSelectWidth,
            DiffSelectHeight,
            content,
            showFooter: false);

        content.BeatmapSelected += (_, beatmap) =>
        {
            selected = beatmap;
            dialog.CloseDialogCommand.Execute(true);
        };

        var result = await ShowAsync(owner, dialog);
        return result is true ? selected : null;
    }

    private static ContentDialog CreateDialog(
        string header,
        double width,
        double height,
        Control content,
        bool showFooter)
    {
        return new ContentDialog
        {
            Width = width,
            Height = height,
            Header = header,
            HeaderShowClose = true,
            ShowFooter = showFooter,
            FooterButtonStyle = showFooter ? FooterButtonStyle.YesNo : FooterButtonStyle.None,
            Content = content
        };
    }

    private static async Task<object?> ShowAsync(Visual? owner, ContentDialog dialog)
    {
        var visual = owner ?? GetMainWindow();
        if (visual == null)
        {
            return false;
        }

        return await visual.ShowContentDialog(dialog, MainWindowDialogIdentifier);
    }
}
