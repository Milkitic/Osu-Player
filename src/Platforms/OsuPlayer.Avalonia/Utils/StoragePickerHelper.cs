using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace OsuPlayer.Utils;

internal static class StoragePickerHelper
{
    private static Window? GetMainWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }

    public static async Task<string?> PickSingleFileAsync(string title, params string[] patterns)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow?.StorageProvider == null)
        {
            return null;
        }

        var filterPatterns = patterns is { Length: > 0 } ? patterns : new[] { "*.*" };
        var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(title)
                {
                    Patterns = filterPatterns
                }
            }
        });

        return files.FirstOrDefault()?.Path.LocalPath;
    }

    public static async Task<string?> PickFolderAsync(string title)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow?.StorageProvider == null)
        {
            return null;
        }

        var folders = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.FirstOrDefault()?.Path.LocalPath;
    }
}
