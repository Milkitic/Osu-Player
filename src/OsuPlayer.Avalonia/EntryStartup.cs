using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper.FluentMap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using OsuPlayer.Avalonia.AnimationOptions;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Data;
using OsuPlayer.Data.Models;
using OsuPlayer.Shared;

namespace OsuPlayer.Avalonia;

public static class EntryStartup
{
    public static async Task StartupAsync(IServiceProvider services)
    {
        LogManager.Setup().SetupExtensions(setup =>
            setup.RegisterLayoutRenderer<InvariantCultureLayoutRendererWrapper>("InvariantCulture"));
        if (!LoadConfig())
        {
            Environment.Exit(0);
            return;
        }

        InitializeAppPaths();

        await InitLocalDbAsync(services);

        // 替代 WPF StyleUtilities.SetAlignment() - Avalonia 不需要此操作
        AnimationOptionsHelper.DisableAnimations = () => AppSettings.Default?.Interface?.MinimalMode == true;
    }

    internal static bool LoadConfig()
    {
        var file = AppPaths.Current.ConfigFile;
        if (!File.Exists(file))
        {
            AppSettings.CreateNewConfig();
        }
        else
        {
            try
            {
                var content = ConcurrentFile.ReadAllText(file);
                AppSettings.Load(JsonSerializer.Deserialize<AppSettings>(content, AppSettings.JsonOptions));
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"Failed to load config: {ex.Message}");
                AppSettings.CreateNewConfig();
            }
        }

        return true;
    }

    private static async Task InitLocalDbAsync(IServiceProvider services)
    {
        FluentMapper.Initialize(config =>
        {
            config.AddMap(new StoryboardInfoMap());
            config.AddMap(new BeatmapMap());
            config.AddMap(new BeatmapSettingsMap());
            config.AddMap(new CollectionMap());
            config.AddMap(new CollectionRelationMap());
        });

        await OsuPlayerDbContext.InitializeDatabaseAsync(
            services.GetRequiredService<Func<OsuPlayerDbContext>>(),
            services.GetRequiredService<ILogger<OsuPlayerDbContext>>());

        var playerData = services.GetRequiredService<IPlayerDataStore>();
        var defCol = await playerData.GetCollectionsAsync();
        var locked = defCol.Where(k => k.LockedBool);
        if (!locked.Any()) await playerData.TryAddCollectionAsync("Favorite", true);
    }

    internal static void InitializeAppPaths()
    {
        var general = AppSettings.Default.General;
        var osuSongPath = general.DbPath == null
            ? null
            : Path.Combine(new FileInfo(general.DbPath).Directory?.FullName ?? string.Empty, "Songs");
        var customSongPath = general.CustomSongsPath == null
            ? null
            : new FileInfo(general.CustomSongsPath).FullName;
        AppPaths.Current = new AppPaths(osuSongPath, customSongPath);
    }
}
