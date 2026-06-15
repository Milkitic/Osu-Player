using System;
using KeyAsio.Core.Audio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Instances;
using OsuPlayer.Core.Scanning;
using OsuPlayer.Core.Services;
using OsuPlayer.Data;
using OsuPlayer.Instances;
using OsuPlayer.Media.Audio;
using OsuPlayer.Playback;
using OsuPlayer.Playback.Playlist;
using OsuPlayer.Pages;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Services;
using OsuPlayer.Shared;
using OsuPlayer.UserControls;
using OsuPlayer.ViewModels;
using OsuPlayer.Windows;

namespace OsuPlayer;

public static class EntrySetup
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLogging(static loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            loggingBuilder.AddNLog();
        });

        services.AddOsuPlayerAudioModule();
        services.Replace(ServiceDescriptor.Singleton<IAudioDeviceManager, OsuPlayerAudioDeviceManager>());
        services.AddDatabaseServices();
        services.AddApplicationServices();
        services.AddViewModels();
        services.AddViews();
        return services;
    }

    private static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        services.AddTransient<OsuPlayerDbContext>(static provider =>
        {
            var options = new DbContextOptionsBuilder<OsuPlayerDbContext>()
                .UseSqlite(OsuPlayerDbContext.DefaultConnectionString)
                .Options;
            return new OsuPlayerDbContext(options, provider.GetRequiredService<ILogger<OsuPlayerDbContext>>());
        });
        services.AddSingleton<Func<OsuPlayerDbContext>>(static provider =>
            () => provider.GetRequiredService<OsuPlayerDbContext>());
        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IBeatmapThumbnailService, WpfBeatmapThumbnailService>();
        services.AddSingleton<IMapModelConverter, MapModelConverter>();
        services.AddSingleton<BeatmapLoader>();

        services.AddSingleton<IAppNotificationService, AppNotificationService>();
        services.AddSingleton<IBeatmapDifficultyPicker, DialogBeatmapDifficultyPicker>();
        services.AddTransient<INavigationService, FrameNavigationService>();
        services.AddSingleton<IUiThreadDispatcher>(_ => Execute.UiThreadDispatcher);
        services.AddSingleton<IUserPreferences>(_ => AppSettings.Default);
        services.AddSingleton<IAppPaths>(_ => AppPaths.Current);

        services.AddSingleton<PlayerDataService>();
        services.AddSingleton<IPlayerDataStore>(static provider => provider.GetRequiredService<PlayerDataService>());
        services.AddSingleton<NotifyingPlayerDataService>();
        services.AddSingleton<IPlayerDataService>(static provider =>
            provider.GetRequiredService<NotifyingPlayerDataService>());

        services.AddSingleton<PlayerEventBus>();
        services.AddSingleton<PlayList>();
        services.AddSingleton<PlayerSessionService>();
        services.AddSingleton<ObservablePlayController>();
        services.AddSingleton<IPlaybackController>(static provider =>
            provider.GetRequiredService<ObservablePlayController>());

        services.AddSingleton<OsuDbInst>();
        services.AddSingleton<LyricsInst>();
        services.AddSingleton<UpdateInst>();
        services.AddSingleton<OsuFileScanner>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IBeatmapActionService, BeatmapActionService>();
        return services;
    }

    private static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddSingleton(_ => SharedVm.Default);
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<CollectionPageViewModel>();
        services.AddTransient<SearchPageViewModel>();
        services.AddSingleton<LyricWindowViewModel>();
        services.AddTransient<RecentPlayPageViewModel>();
        services.AddTransient<ExportPageViewModel>();
        services.AddTransient<PlayListControlVm>();
        services.AddTransient<InterfacePageViewModel>();
        services.AddTransient<AboutPageViewModel>();
        services.AddTransient<GeneralPageViewModel>();
        services.AddTransient<PlayPageViewModel>();
        return services;
    }

    private static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<LyricWindow>();
        services.AddTransient<ConfigWindow>();
        services.AddTransient<MiniWindow>();
        services.AddTransient<CollectionPage>();
        services.AddTransient<SearchPage>();
        services.AddTransient<RecentPlayPage>();
        services.AddTransient<ExportPage>();
        services.AddTransient<StoryboardPage>();

        services.AddTransient<Pages.Settings.AboutPage>();
        services.AddTransient<Pages.Settings.ExportPage>();
        services.AddTransient<Pages.Settings.GeneralPage>();
        services.AddTransient<Pages.Settings.HotKeyPage>();
        services.AddTransient<Pages.Settings.InterfacePage>();
        services.AddTransient<Pages.Settings.LyricPage>();
        services.AddTransient<Pages.Settings.PlayPage>();
        return services;
    }
}
