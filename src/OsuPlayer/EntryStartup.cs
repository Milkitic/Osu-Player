using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Dapper.FluentMap;
using FFmpeg.AutoGen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Data;
using OsuPlayer.Data.Models;
using OsuPlayer.Presentation;
using OsuPlayer.Shared;

namespace OsuPlayer;

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

        Domain.OsuPathProvider = () =>
            AppSettings.Default?.General?.DbPath == null
                ? null
                : new System.IO.FileInfo(AppSettings.Default.General.DbPath).Directory?.FullName;
        Domain.CustomSongPathProvider = () =>
            AppSettings.Default?.General?.CustomSongsPath == null
                ? null
                : new System.IO.FileInfo(AppSettings.Default.General.CustomSongsPath).FullName;

#if DEBUG
        //ConsoleManager.Show();
#endif

        await InitLocalDbAsync(services);

        StyleUtilities.SetAlignment();

        InitFFmpeg();
    }

    internal static bool LoadConfig()
    {
        var file = Domain.ConfigFile;
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
                var result = MessageBox.Show("载入配置文件时失败，用默认配置覆盖继续打开吗？" + Environment.NewLine + ex.Message,
                    "Osu Player", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    AppSettings.CreateNewConfig();
                }
                else
                    return false;
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

    private static void InitFFmpeg()
    {
        var ffmpegArchitecture = Environment.Is64BitProcess ? "win-x64" : "win-x86";
        var ffmpegDirectory = Path.Combine(Domain.PluginPath, "ffmpeg", ffmpegArchitecture);

        Unosquare.FFME.Library.FFmpegDirectory = ffmpegDirectory;
        DynamicallyLoadedBindings.FunctionResolver = new FFmpegWindowsFunctionResolver();

        if (!Unosquare.FFME.Library.LoadFFmpeg())
        {
            throw new DllNotFoundException($"Unable to initialize FFmpeg from '{ffmpegDirectory}'.");
        }

        _ = ffmpeg.avformat_version();
    }
}
