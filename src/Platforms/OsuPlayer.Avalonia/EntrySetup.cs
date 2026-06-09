using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Instances;
using OsuPlayer.Core.Scanning;
using OsuPlayer.Core.Services;
using OsuPlayer.Data;
using OsuPlayer.Media.Audio;
using OsuPlayer.Playback;
using OsuPlayer.Playback.Playlist;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Services;
using OsuPlayer.Shared;
using OsuPlayer.ViewModels;
using OsuPlayer.ViewModels.Pages.Settings;
using OsuPlayer.Views.Pages;
using OsuPlayer.Views.Pages.Settings;
using ExportPage = OsuPlayer.Views.Pages.Settings.ExportPage;
using ExportPageViewModel = OsuPlayer.ViewModels.Pages.Settings.ExportPageViewModel;

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

        services.AddDatabaseServices();
        services.AddApplicationServices();
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
        services.AddSingleton<IBeatmapThumbnailService, BeatmapThumbnailService>();
        services.AddSingleton<IMapModelConverter, MapModelConverter>();
        services.AddSingleton<BeatmapLoader>();

        services.AddSingleton<IAppNotificationService>(AppNotificationService.Instance);
        services.AddTransient<INavigationService, FrameNavigationService>();
        services.AddSingleton<IUiThreadDispatcher>(_ => Execute.UiThreadDispatcher);
        services.AddSingleton<IUserPreferences>(_ =>
            AppSettings.Default ?? throw new InvalidOperationException("AppSettings is not loaded."));
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
        services.AddSingleton<UpdateInst>();
        services.AddSingleton<OsuFileScanner>();

        services.AddSingleton<MainWindowViewModel>();

        // Settings 页面
        services.AddTransient<InterfacePage>();
        services.AddTransient<AboutPage>();
        services.AddTransient<GeneralPage>();
        services.AddTransient<PlayPage>();
        services.AddTransient<LyricPage>();
        services.AddTransient<ExportPage>();
        services.AddTransient<HotKeyPage>();

        // Settings ViewModels
        services.AddTransient<InterfacePageViewModel>();
        services.AddTransient<AboutPageViewModel>();
        services.AddTransient<GeneralPageViewModel>();
        services.AddTransient<PlayPageViewModel>();
        services.AddTransient<LyricPageViewModel>();
        services.AddTransient<ExportPageViewModel>();
        services.AddTransient<HotKeyPageViewModel>();

        // 主页面
        services.AddTransient<CollectionPage>();
        services.AddTransient<RecentPlayPage>();
        services.AddTransient<SearchPage>();
        services.AddTransient<FindPage>();
        services.AddTransient<Views.Pages.ExportPage>();

        // 主页面 ViewModels
        services.AddTransient<CollectionPageViewModel>();
        services.AddTransient<RecentPlayPageViewModel>();
        services.AddTransient<SearchPageViewModel>();
        services.AddTransient<ViewModels.ExportPageViewModel>();
        return services;
    }
}
