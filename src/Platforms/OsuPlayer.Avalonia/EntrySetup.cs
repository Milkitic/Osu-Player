using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using OsuPlayer.Avalonia.AnimationOptions;
using OsuPlayer.Avalonia.Interaction;
using OsuPlayer.Avalonia.Services;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Instances;
using OsuPlayer.Core.Scanning;
using OsuPlayer.Core.Services;
using OsuPlayer.Data;
using OsuPlayer.Media.Audio;
using OsuPlayer.Playback;
using OsuPlayer.Playback.Playlist;
using OsuPlayer.Shared;

namespace OsuPlayer.Avalonia;

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

        services.AddSingleton<OsuPlayer.Avalonia.ViewModels.MainWindowViewModel>();

        // Settings 页面
        services.AddTransient<OsuPlayer.Avalonia.Views.Pages.Settings.InterfacePage>();
        services.AddTransient<OsuPlayer.Avalonia.Views.Pages.Settings.AboutPage>();
        services.AddTransient<OsuPlayer.Avalonia.Views.Pages.Settings.GeneralPage>();
        services.AddTransient<OsuPlayer.Avalonia.Views.Pages.Settings.PlayPage>();
        services.AddTransient<OsuPlayer.Avalonia.Views.Pages.Settings.LyricPage>();
        services.AddTransient<OsuPlayer.Avalonia.Views.Pages.Settings.ExportPage>();
        services.AddTransient<OsuPlayer.Avalonia.Views.Pages.Settings.HotKeyPage>();

        // Settings ViewModels
        services.AddTransient<OsuPlayer.Avalonia.ViewModels.Pages.Settings.InterfacePageViewModel>();
        services.AddTransient<OsuPlayer.Avalonia.ViewModels.Pages.Settings.AboutPageViewModel>();
        services.AddTransient<OsuPlayer.Avalonia.ViewModels.Pages.Settings.GeneralPageViewModel>();
        services.AddTransient<OsuPlayer.Avalonia.ViewModels.Pages.Settings.PlayPageViewModel>();
        services.AddTransient<OsuPlayer.Avalonia.ViewModels.Pages.Settings.LyricPageViewModel>();
        services.AddTransient<OsuPlayer.Avalonia.ViewModels.Pages.Settings.ExportPageViewModel>();
        services.AddTransient<OsuPlayer.Avalonia.ViewModels.Pages.Settings.HotKeyPageViewModel>();

        // 主页面
        services.AddTransient<OsuPlayer.Avalonia.Views.Pages.CollectionPage>();
        services.AddTransient<OsuPlayer.Avalonia.Views.Pages.RecentPlayPage>();
        services.AddTransient<OsuPlayer.Avalonia.Views.Pages.SearchPage>();
        services.AddTransient<OsuPlayer.Avalonia.Views.Pages.FindPage>();
        services.AddTransient<OsuPlayer.Avalonia.Views.Pages.ExportPage>();

        // 主页面 ViewModels
        services.AddTransient<OsuPlayer.Avalonia.ViewModels.CollectionPageViewModel>();
        services.AddTransient<OsuPlayer.Avalonia.ViewModels.RecentPlayPageViewModel>();
        services.AddTransient<OsuPlayer.Avalonia.ViewModels.SearchPageViewModel>();
        services.AddTransient<OsuPlayer.Avalonia.ViewModels.ExportPageViewModel>();
        return services;
    }
}
